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
            var midiChannel = (short)channelMessage.MidiChannel;

            // TODO: Midi Stop All see https://forums.steinberg.net/t/vst3-velocity-clarification/908883
            if (channelMessage.IsNoteOn())
            {
                var e = new NoteOnEvent(
                    channel: (short)channelMessage.MidiChannel,
                    pitch: (short)channelMessage.Data1,
                    tuning: default,
                    velocity: MessageUtils.MidiIntToFloat(channelMessage.Data2),
                    length: default,
                    noteId: channelMessage.Data1);
                inputEventQueue.Add(Event.New(e, busIndex: 0, sampleOffset: 0, ppqPosition: 0, isLive: false));
            }
            else if (channelMessage.IsNoteOff())
            {
                var e = new NoteOffEvent(
                    channel: (short)channelMessage.MidiChannel,
                    pitch: (short)channelMessage.Data1,
                    tuning: default,
                    velocity: MessageUtils.MidiIntToFloat(channelMessage.Data2),
                    noteId: channelMessage.Data1);
                inputEventQueue.Add(Event.New(e, busIndex: 0, sampleOffset: 0, ppqPosition: 0, isLive: false));
            }
            else
            {
                // IMidiMapping and IMidiLearn expect to be called on main thread
                synchronizationContext?.Post(m => HandleMidiMessageOnMainThread((ChannelMessage)m!), channelMessage);
            }
        }
    }

    private void HandleMidiMessageOnMainThread(ChannelMessage channelMessage)
    {
        var midiChannel = (short)channelMessage.MidiChannel;
        var midiMapping = controller as IMidiMapping;

        if (channelMessage.Command == ChannelCommand.Controller)
        {
            var midiLearn = controller as IMidiLearn;
            if (midiLearn != null)
            {
                if (midiLearn.onLiveMIDIControllerInput(0, midiChannel, (ControllerNumbers)channelMessage.Data1))
                {
                    // Plugin did map the controller to one of its parameters
                }
            }
            if (midiMapping != null && midiMapping.getMidiControllerAssignment(0, midiChannel, (ControllerNumbers)channelMessage.Data1, out var paramId))
            {
                var value = MessageUtils.MidiIntToFloat(channelMessage.Data2);
                SetParameter(paramId, value);
            }
        }
        else if (channelMessage.Command == ChannelCommand.PitchWheel)
        {
            if (midiMapping is null)
                return;

            if (midiMapping.getMidiControllerAssignment(0, midiChannel, ControllerNumbers.kPitchBend, out var paramId))
            {
                var value = channelMessage.GetPitchWheel();
                SetParameter(paramId, value);
            }
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
