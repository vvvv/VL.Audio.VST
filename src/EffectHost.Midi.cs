using Microsoft.Extensions.Logging;
using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Audio.VST.Internal;
using VL.Lib.IO.Midi;
using VST3;

namespace VL.Audio.VST;

partial class EffectHost
{
    private void HandleMidiMessage(IMidiMessage message)
    {
        if (message is ChannelMessage channelMessage)
        {
            if (TryTranslateToEvent(channelMessage, out var e))
                inputEventQueue.Add(e);
            else
                // IMidiMapping and IMidiLearn expect to be called on main thread
                synchronizationContext?.Post(m => HandleMidiMessageOnMainThread((ChannelMessage)m!), channelMessage);
        }
    }

    /// <summary>
    /// Translates note on, note off and poly pressure to the event the plugin receives in process; false for other messages.
    /// </summary>
    internal static bool TryTranslateToEvent(ChannelMessage channelMessage, out Event e)
    {
        var midiChannel = (short)channelMessage.MidiChannel;

        // TODO: Midi Stop All see https://forums.steinberg.net/t/vst3-velocity-clarification/908883
        if (channelMessage.IsNoteOn())
        {
            var noteOn = new NoteOnEvent(
                channel: midiChannel,
                pitch: (short)channelMessage.Data1,
                tuning: default,
                velocity: (float)MidiConvert.Midi7BitToNormalized(channelMessage.Data2),
                length: default,
                noteId: NoteIdOf(channelMessage.MidiChannel, channelMessage.Data1));
            e = Event.New(noteOn, busIndex: 0, sampleOffset: 0, ppqPosition: 0, isLive: false);
            return true;
        }
        if (channelMessage.IsNoteOff())
        {
            var noteOff = new NoteOffEvent(
                channel: midiChannel,
                pitch: (short)channelMessage.Data1,
                tuning: default,
                velocity: (float)MidiConvert.Midi7BitToNormalized(channelMessage.Data2),
                noteId: NoteIdOf(channelMessage.MidiChannel, channelMessage.Data1));
            e = Event.New(noteOff, busIndex: 0, sampleOffset: 0, ppqPosition: 0, isLive: false);
            return true;
        }
        if (channelMessage.Command == ChannelCommand.PolyPressure)
        {
            var polyPressure = new PolyPressureEvent(
                channel: midiChannel,
                pitch: (short)channelMessage.Data1,
                pressure: (float)MidiConvert.Midi7BitToNormalized(channelMessage.Data2),
                noteId: NoteIdOf(channelMessage.MidiChannel, channelMessage.Data1));
            e = Event.New(polyPressure, busIndex: 0, sampleOffset: 0, ppqPosition: 0, isLive: false);
            return true;
        }
        e = default;
        return false;
    }

    // Unique per sounding note, so the same key on different MPE channels stays independent
    private static int NoteIdOf(int channel, int key) => channel * 128 + key;

    /// <summary>
    /// Translates controller, pitch bend and channel pressure to the controller number a plugin maps to a parameter
    /// (IMidiMapping) and the normalized value; false for other messages.
    /// </summary>
    internal static bool TryTranslateToController(ChannelMessage channelMessage, out ControllerNumbers controllerNumber, out double value)
    {
        switch (channelMessage.Command)
        {
            case ChannelCommand.Controller:
                controllerNumber = (ControllerNumbers)channelMessage.Data1;
                value = MidiConvert.Midi7BitToNormalized(channelMessage.Data2);
                return true;
            case ChannelCommand.PitchWheel:
                controllerNumber = ControllerNumbers.kPitchBend;
                value = MidiConvert.Midi14BitToNormalized(channelMessage.Data1 | channelMessage.Data2 << 7);
                return true;
            case ChannelCommand.ChannelPressure:
                controllerNumber = ControllerNumbers.kAfterTouch;
                value = MidiConvert.Midi7BitToNormalized(channelMessage.Data1);
                return true;
            default:
                controllerNumber = default;
                value = default;
                return false;
        }
    }

    private void HandleMidiMessageOnMainThread(ChannelMessage channelMessage)
    {
        var midiChannel = (short)channelMessage.MidiChannel;

        if (channelMessage.Command == ChannelCommand.Controller && controller is IMidiLearn midiLearn)
        {
            if (midiLearn.onLiveMIDIControllerInput(0, midiChannel, (ControllerNumbers)channelMessage.Data1))
            {
                // Plugin did map the controller to one of its parameters
            }
        }

        if (TryTranslateToController(channelMessage, out var controllerNumber, out var normalizedValue))
        {
            if (controller is IMidiMapping midiMapping && midiMapping.getMidiControllerAssignment(0, midiChannel, controllerNumber, out var paramId))
                SetParameter(paramId, normalizedValue);
        }
        else if (channelMessage.Command == ChannelCommand.ProgramChange)
        {
            if (controller is null)
                return;

            var unitController = controller as IUnitInfo;
            if (unitController is null)
                return;

            try
            {
                if (!unitController.getUnitByBus(MediaTypes.kEvent, BusDirections.kInput, 0, channelMessage.MidiChannel, out var unitId))
                    return;

                var parameter = controller.GetParameters()
                    .FirstOrDefault(p => p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsProgramChange) && p.UnitId == unitId);
                if (parameter.ID > 0)
                {
                    var value = Utils.Normalize(channelMessage.Data1, parameter.StepCount);
                    SetParameter(parameter.ID, value);
                }
            }
            catch (NotImplementedException)
            {

            }
        }
    }

    bool TryTranslateToMidi(in Event e, [NotNullWhen(true)] out IMidiMessage? message)
    {
        switch (e.Type)
        {
            case Event.EventTypes.kNoteOnEvent:
                var noteOn = e.GetValue<NoteOnEvent>();
                message = MessageUtils.NoteOn(noteOn.Channel, noteOn.Pitch, noteOn.Velocity);
                return true;
            case Event.EventTypes.kNoteOffEvent:
                var noteOff = e.GetValue<NoteOffEvent>();
                message = MessageUtils.NoteOn(noteOff.Channel, noteOff.Pitch, noteOff.Velocity);
                return true;
            case Event.EventTypes.kDataEvent:
                var dataEvent = e.GetValue<DataEvent>();
                switch (dataEvent.Type)
                {
                    case DataEvent.DataTypes.kMidiSysEx:
                        message = new SysExMessage(dataEvent.DataBlock.ToArray());
                        return true;
                    default:
                        break;
                }
                break;
            case Event.EventTypes.kPolyPressureEvent:
            case Event.EventTypes.kNoteExpressionValueEvent:
            case Event.EventTypes.kNoteExpressionTextEvent:
            case Event.EventTypes.kChordEvent:
            case Event.EventTypes.kScaleEvent:
                logger.LogTrace("Not implemented {type}", e.Type);
                break;
            case Event.EventTypes.kLegacyMIDICCOutEvent:
                var midiCCEvent = e.GetValue<LegacyMIDICCOutEvent>();
                switch (midiCCEvent.ControlNumber)
                {
                    case ControllerNumbers.kCtrlProgramChange:
                        message = new ChannelMessage(ChannelCommand.ProgramChange, midiCCEvent.Channel, midiCCEvent.Value);
                        return true;
                    case ControllerNumbers.kCtrlPolyPressure:
                        message = new ChannelMessage(ChannelCommand.PolyPressure, midiCCEvent.Channel, midiCCEvent.Value, midiCCEvent.Value2);
                        return true;
                    case ControllerNumbers.kCtrlQuarterFrame:
                        logger.LogTrace("Not implemented kCtrlQuarterFrame");
                        break;
                    default:
                        message = MessageUtils.Controller(midiCCEvent.Channel, (int)midiCCEvent.ControlNumber, midiCCEvent.Value);
                        return true;
                }
                break;
            default:
                logger.LogTrace("Not implemented {type}", e.Type);
                break;
        }

        message = null;
        return false;
    }
}
