using Sanford.Multimedia.Midi;
using VST3;

namespace VL.Audio.VST.Tests;

/// <summary>
/// What the plugin sends in process to MIDI out (<see cref="EffectHost.TryTranslateToMidi"/>).
/// </summary>
public class VstToMidiTests
{
    private static ChannelMessage ToMidi<TEvent>(TEvent e) where TEvent : unmanaged, IEvent
    {
        var vstEvent = Event.New(e, busIndex: 0, sampleOffset: 0, ppqPosition: 0, isLive: false);
        Assert.That(EffectHost.TryTranslateToMidi(in vstEvent, out var message), Is.True);
        Assert.That(message, Is.InstanceOf<ChannelMessage>());
        return (ChannelMessage)message!;
    }

    [Test]
    public void NoteOn_BecomesNoteOn()
    {
        var m = ToMidi(new NoteOnEvent(channel: 3, pitch: 60, tuning: 0, velocity: 1f, length: 0, noteId: -1));

        Assert.That(m.Command, Is.EqualTo(ChannelCommand.NoteOn));
        Assert.That(m.MidiChannel, Is.EqualTo(3));
        Assert.That(m.Data1, Is.EqualTo(60));
        Assert.That(m.Data2, Is.EqualTo(127));
    }

    // Task tpj7: a note off was sent as a note on with the release velocity, so a release velocity above 0 started a new note
    [Test]
    public void NoteOff_BecomesNoteOff_WithItsReleaseVelocity()
    {
        var m = ToMidi(new NoteOffEvent(channel: 3, pitch: 60, velocity: 0.5f, noteId: -1, tuning: 0));

        Assert.That(m.Command, Is.EqualTo(ChannelCommand.NoteOff));
        Assert.That(m.MidiChannel, Is.EqualTo(3));
        Assert.That(m.Data1, Is.EqualTo(60));
        Assert.That(m.Data2, Is.InRange(63, 64));
    }

    [Test]
    public void NoteOff_WithZeroVelocity_BecomesNoteOff()
    {
        var m = ToMidi(new NoteOffEvent(channel: 0, pitch: 42, velocity: 0f, noteId: -1, tuning: 0));

        Assert.That(m.Command, Is.EqualTo(ChannelCommand.NoteOff));
        Assert.That(m.Data1, Is.EqualTo(42));
        Assert.That(m.Data2, Is.EqualTo(0));
    }
}
