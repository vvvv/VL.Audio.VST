using Sanford.Multimedia.Midi;
using VST3;

namespace VL.Audio.VST.Tests;

/// <summary>
/// MIDI in to what the plugin receives: events in process, or a controller value the plugin maps to a parameter.
/// </summary>
/// <remarks>
/// Ported from the VST3 SDK 3.8.1 (MIT), public.sdk/source/vst/hosting/test/miditovsttest.cpp, which tests
/// midiToEvent and midiToParameter of public.sdk/samples/vst-hosting/audiohost/source/media/miditovst.h. Our
/// counterparts are <see cref="EffectHost.TryTranslateToEvent"/> and <see cref="EffectHost.TryTranslateToController"/>;
/// the mapping of a controller to a parameter (toParamID in the SDK) is the plugin's IMidiMapping, which EffectHost
/// asks. Values are expected exactly as in the SDK.
/// </remarks>
public class MidiToVstTests
{
    // The SDK leaves the note ID at -1 (note on/off) or 0 (poly pressure); we give every sounding note its own ID,
    // channel * 128 + key, so the same key on different MPE channels stays independent (#15)
    private static int NoteIdOf(int channel, int key) => channel * 128 + key;

    private static Event ToEvent(ChannelMessage message)
    {
        Assert.That(EffectHost.TryTranslateToEvent(message, out var e), Is.True, $"{message.Command}");
        return e;
    }

    // midiToEvent noteOn
    [TestCase(0, 60, 64, 0.5f)]
    [TestCase(6, 94, 127, 1f)]
    public void MidiToEvent_NoteOn(int channel, int pitch, int velocity, float expectedVelocity)
    {
        var e = ToEvent(new ChannelMessage(ChannelCommand.NoteOn, channel, pitch, velocity));

        Assert.That(e.Type, Is.EqualTo(Event.EventTypes.kNoteOnEvent));
        var noteOn = e.GetValue<NoteOnEvent>();
        Assert.That(noteOn.Channel, Is.EqualTo(channel));
        Assert.That(noteOn.Pitch, Is.EqualTo(pitch));
        Assert.That(noteOn.Velocity, Is.EqualTo(expectedVelocity));
        Assert.That(noteOn.NoteId, Is.EqualTo(NoteIdOf(channel, pitch)));
    }

    // midiToEvent noteOff
    [TestCase(0, 60, 64, 0.5f)]
    [TestCase(6, 94, 127, 1f)]
    public void MidiToEvent_NoteOff(int channel, int pitch, int velocity, float expectedVelocity)
    {
        var e = ToEvent(new ChannelMessage(ChannelCommand.NoteOff, channel, pitch, velocity));

        Assert.That(e.Type, Is.EqualTo(Event.EventTypes.kNoteOffEvent));
        var noteOff = e.GetValue<NoteOffEvent>();
        Assert.That(noteOff.Channel, Is.EqualTo(channel));
        Assert.That(noteOff.Pitch, Is.EqualTo(pitch));
        Assert.That(noteOff.Velocity, Is.EqualTo(expectedVelocity));
        Assert.That(noteOff.NoteId, Is.EqualTo(NoteIdOf(channel, pitch)));
    }

    // Not in the SDK test (its midiToEvent sends a note on with velocity 0): a note on with velocity 0 is a note off
    [Test]
    public void MidiToEvent_NoteOnWithVelocityZero_IsANoteOff()
    {
        var e = ToEvent(new ChannelMessage(ChannelCommand.NoteOn, 2, 60, 0));

        Assert.That(e.Type, Is.EqualTo(Event.EventTypes.kNoteOffEvent));
        var noteOff = e.GetValue<NoteOffEvent>();
        Assert.That((noteOff.Channel, noteOff.Pitch, noteOff.Velocity, noteOff.NoteId), Is.EqualTo(((short)2, (short)60, 0f, NoteIdOf(2, 60))));
    }

    // midiToEvent polyPressure
    [TestCase(0, 60, 64, 0.5f)]
    [TestCase(6, 94, 127, 1f)]
    public void MidiToEvent_PolyPressure(int channel, int pitch, int pressure, float expectedPressure)
    {
        var e = ToEvent(new ChannelMessage(ChannelCommand.PolyPressure, channel, pitch, pressure));

        Assert.That(e.Type, Is.EqualTo(Event.EventTypes.kPolyPressureEvent));
        var polyPressure = e.GetValue<PolyPressureEvent>();
        Assert.That(polyPressure.Channel, Is.EqualTo(channel));
        Assert.That(polyPressure.Pitch, Is.EqualTo(pitch));
        Assert.That(polyPressure.Pressure, Is.EqualTo(expectedPressure));
        Assert.That(polyPressure.NoteId, Is.EqualTo(NoteIdOf(channel, pitch)));
    }

    // midiToEvent unsupported
    [Test]
    public void MidiToEvent_Unsupported()
    {
        Assert.That(EffectHost.TryTranslateToEvent(new ChannelMessage(ChannelCommand.Controller, 3, 44, 77), out _), Is.False);
        Assert.That(EffectHost.TryTranslateToEvent(new ChannelMessage(ChannelCommand.ProgramChange, 9, 1), out _), Is.False);
        Assert.That(EffectHost.TryTranslateToEvent(new ChannelMessage(ChannelCommand.ChannelPressure, 11, 99), out _), Is.False);
        Assert.That(EffectHost.TryTranslateToEvent(new ChannelMessage(ChannelCommand.PitchWheel, 2, 12, 34), out _), Is.False);
    }

    // midiToParameter controller
    [TestCase(7, 88, 64, 0.5)]
    [TestCase(6, 94, 127, 1.0)]
    public void MidiToParameter_Controller(int channel, int controller, int value, double expected)
    {
        Assert.That(EffectHost.TryTranslateToController(new ChannelMessage(ChannelCommand.Controller, channel, controller, value), out var number, out var normalized), Is.True);
        Assert.That(number, Is.EqualTo((ControllerNumbers)controller));
        Assert.That(normalized, Is.EqualTo(expected));
    }

    // midiToParameter pitchBend: LSB, MSB
    [TestCase(1, 0x00, 0x00, 0.0)]
    [TestCase(1, 0x00, 0x10, 0.125)]
    [TestCase(1, 0x00, 0x20, 0.25)]
    [TestCase(1, 0x00, 0x30, 0.375)]
    [TestCase(7, 0x00, 0x40, 0.5)]
    [TestCase(6, 0x7F, 0x7F, 1.0)]
    public void MidiToParameter_PitchBend(int channel, int lsb, int msb, double expected)
    {
        Assert.That(EffectHost.TryTranslateToController(new ChannelMessage(ChannelCommand.PitchWheel, channel, lsb, msb), out var number, out var normalized), Is.True);
        Assert.That(number, Is.EqualTo(ControllerNumbers.kPitchBend));
        Assert.That(normalized, Is.EqualTo(expected));
    }

    [Test]
    public void MidiToParameter_PitchBend_NearThreeQuarters()
    {
        Assert.That(EffectHost.TryTranslateToController(new ChannelMessage(ChannelCommand.PitchWheel, 6, 0x7F, 0x5F), out var number, out var normalized), Is.True);
        Assert.That(number, Is.EqualTo(ControllerNumbers.kPitchBend));
        Assert.That(normalized, Is.EqualTo(0.75).Within(1e-3));
    }

    // midiToParameter channelPressure
    [TestCase(7, 64, 0.5)]
    [TestCase(6, 127, 1.0)]
    public void MidiToParameter_ChannelPressure(int channel, int pressure, double expected)
    {
        Assert.That(EffectHost.TryTranslateToController(new ChannelMessage(ChannelCommand.ChannelPressure, channel, pressure), out var number, out var normalized), Is.True);
        Assert.That(number, Is.EqualTo(ControllerNumbers.kAfterTouch));
        Assert.That(normalized, Is.EqualTo(expected));
    }

    // midiToParameter unsupported. Two SDK cases have no counterpart here: pitch bend without a toParamID function is
    // EffectHost with a controller that has no IMidiMapping, and status 130 (0x82) is a note off on channel 2 in our
    // message type, covered by the note off case.
    [Test]
    public void MidiToParameter_Unsupported()
    {
        Assert.That(EffectHost.TryTranslateToController(new ChannelMessage(ChannelCommand.NoteOn, 3, 44, 77), out _, out _), Is.False);
        Assert.That(EffectHost.TryTranslateToController(new ChannelMessage(ChannelCommand.NoteOff, 9, 1, 2), out _, out _), Is.False);
        Assert.That(EffectHost.TryTranslateToController(new ChannelMessage(ChannelCommand.PolyPressure, 11, 99, 0), out _, out _), Is.False);
        Assert.That(EffectHost.TryTranslateToController(new ChannelMessage(ChannelCommand.ProgramChange, 2, 12), out _, out _), Is.False);
    }
}
