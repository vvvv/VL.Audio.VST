using Sanford.Multimedia.Midi;
using System.Collections.Immutable;
using System.Runtime.InteropServices.Marshalling;
using System.Windows.Forms;
using VL.Audio.VST.Internal;
using VL.Audio.VST.Tests.Fakes;
using VST3;
using VST3.Hosting;

namespace VL.Audio.VST.Tests;

/// <summary>
/// The host talks to <see cref="FakePlugin"/> through native COM pointers, the way it talks to a real plugin.
/// </summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
public partial class HostInteractionTests
{
    private const uint GainId = 7;

    private SynchronizationContext? previousContext;
    private WindowsFormsSynchronizationContext? context;

    // VstWrappers needs a synchronization context on the thread that creates and releases the wrappers, as in vvvv
    [SetUp]
    public void SetUp()
    {
        previousContext = SynchronizationContext.Current;
        context = new WindowsFormsSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);
    }

    [TearDown]
    public void TearDown()
    {
        VstWrappers.Instance.ReleaseRemainingComObjects();
        SynchronizationContext.SetSynchronizationContext(previousContext);
        context?.Dispose();
    }

    private sealed class HostedPlugin : IDisposable
    {
        private readonly PluginFactory factory;

        public HostedPlugin(FakePlugin fake)
        {
            Fake = fake;
            factory = ComRoundTrip.CreateFactory(fake);
            Provider = PlugProvider.Create(factory, FakePlugin.ClassId, new HostApp([]))!;
        }

        public FakePlugin Fake { get; }
        public PlugProvider Provider { get; }
        public IComponent Component => Provider.Component;
        public IEditController Controller => Provider.Controller!;
        public IAudioProcessor Processor => (IAudioProcessor)Provider.Component;

        public void Dispose()
        {
            Provider.Dispose();
            factory.Dispose();
        }
    }

    [GeneratedComClass]
    internal sealed partial class RecordingHandler : IComponentHandler
    {
        public Action<RestartFlags>? OnRestart { get; set; }
        public List<RestartFlags> Restarts { get; } = new();

        public void beginEdit(uint id) { }
        public void performEdit(uint id, double valueNormalized) { }
        public void endEdit(uint id) { }

        public void restartComponent(RestartFlags flags)
        {
            Restarts.Add(flags);
            OnRestart?.Invoke(flags);
        }
    }

    private static ParameterInfo P(uint id, string title, int unitId = Constants.kRootUnitId, int stepCount = 0)
        => new(id, title, stepCount: stepCount, unitId: unitId);

    [Test]
    public void PlugProvider_CreatesAndConnectsComponentAndController()
    {
        var fake = new FakePlugin();
        using (var plugin = new HostedPlugin(fake))
        {
            // The host holds wrappers of native pointers, not the fake itself
            Assert.That(plugin.Component, Is.InstanceOf<ComObject>());
            Assert.That(plugin.Controller, Is.InstanceOf<ComObject>());
            Assert.That(plugin.Provider.Id, Is.EqualTo(FakePlugin.ClassId));

            Assert.That(fake.Component.IsInitialized, Is.True);
            Assert.That(fake.Controller.IsInitialized, Is.True);
            Assert.That(fake.Component.ConnectedTo, Is.Not.Null);
            Assert.That(fake.Controller.ConnectedTo, Is.Not.Null);
        }

        Assert.That(fake.Component.ConnectedTo, Is.Null);
        Assert.That(fake.Controller.ConnectedTo, Is.Null);
        Assert.That(fake.Component.IsTerminated, Is.True);
        Assert.That(fake.Controller.IsTerminated, Is.True);
    }

    [Test]
    public void GetParameters_ReturnsWhatThePluginReports()
    {
        var fake = new FakePlugin();
        fake.Controller.Units.Add(new UnitInfo(1, Constants.kRootUnitId, "Amp"));
        fake.Controller.Parameters.Add(new ParameterInfo(GainId, "Gain", stepCount: 0, defaultNormalizedValue: 0.25, unitId: 1, shortTitle: "G", units: "dB"));
        fake.Controller.Parameters.Add(new ParameterInfo(8, "Bypass", stepCount: 1, flags: ParameterInfo.ParameterFlags.kIsBypass | ParameterInfo.ParameterFlags.kCanAutomate));
        using var plugin = new HostedPlugin(fake);

        var parameters = plugin.Controller.GetParameters().ToList();
        var units = ((IUnitInfo)plugin.Controller).GetUnitInfos().ToList();

        Assert.That(parameters.Select(p => (p.ID, p.Title, p.ShortTitle, p.Units, p.StepCount, p.DefaultNormalizedValue, p.UnitId)),
            Is.EqualTo(new[] { (GainId, "Gain", "G", "dB", 0, 0.25, 1), (8u, "Bypass", "", "", 1, 0.0, 0) }));
        Assert.That(parameters[1].Flags, Is.EqualTo(ParameterInfo.ParameterFlags.kIsBypass | ParameterInfo.ParameterFlags.kCanAutomate));
        Assert.That(units.Select(u => (u.Id, u.ParentUnitId, u.Name)), Is.EqualTo(new[] { (1, Constants.kRootUnitId, "Amp") }));

        var pins = new ParameterPins();
        pins.SetParameters(parameters, units);
        Assert.That(pins.TryResolve("Amp.Gain", out uint id) && id == GainId, Is.True);
    }

    [Test]
    public void ParametersPublishedLate_ResolveAPinAfterRestartComponent()
    {
        var fake = new FakePlugin();
        using var plugin = new HostedPlugin(fake);

        // As EffectHost: reload the parameters when the titles changed, then sync the pins
        var pins = new ParameterPins { SavedIds = ImmutableDictionary<string, uint>.Empty.Add("Gain", GainId) };
        var values = new Dictionary<string, object> { ["Gain"] = 0.5f };
        PinGroupState? state = null;
        void Reload()
        {
            pins.SetParameters(plugin.Controller.GetParameters(), ((IUnitInfo)plugin.Controller).GetUnitInfos());
            state = pins.Sync(values);
            pins.SavedIds = state.SavedIds;
        }
        var handler = new RecordingHandler
        {
            OnRestart = flags =>
            {
                if (flags.HasFlag(RestartFlags.kParamTitlesChanged))
                    Reload();
            }
        };
        plugin.Controller.setComponentHandler(handler);
        Assert.That(fake.Controller.Handler, Is.Not.Null);

        Reload();
        Assert.That(state!.Warning, Is.EqualTo("No parameter found for pins: \"Gain\""));
        Assert.That(state.Pins, Is.EqualTo(new[] { ("Gain", typeof(float)) }));

        fake.Controller.Parameters.Add(P(GainId, "Gain"));
        fake.Controller.RestartComponent(RestartFlags.kParamTitlesChanged);

        Assert.That(handler.Restarts, Is.EqualTo(new[] { RestartFlags.kParamTitlesChanged }));
        Assert.That(state!.Warning, Is.Null);
        Assert.That(pins.TryResolve("Gain", out uint id) && id == GainId, Is.True);

        // Renamed in the plugin: the pin stays bound by its ID
        fake.Controller.Rename(GainId, "Volume");
        fake.Controller.RestartComponent(RestartFlags.kParamTitlesChanged);

        Assert.That(state!.Warning, Is.Null);
        Assert.That(state.SavedIds, Is.EquivalentTo(new Dictionary<string, uint> { ["Gain"] = GainId }));
        Assert.That(pins.TryResolve("Gain", out id) && id == GainId, Is.True);

        // Removed in the plugin: the pin stays and warns
        fake.Controller.Parameters.Clear();
        fake.Controller.RestartComponent(RestartFlags.kParamTitlesChanged);

        Assert.That(state!.Warning, Is.EqualTo("No parameter found for pins: \"Gain\""));
        Assert.That(state.Pins, Is.EqualTo(new[] { ("Gain", typeof(float)) }));
        Assert.That(state.SavedIds, Is.EquivalentTo(new Dictionary<string, uint> { ["Gain"] = GainId }));
    }

    [Test]
    public void PluginState_RoundTripsTheComponentState()
    {
        byte[] bytes = [1, 2, 3, 4, 5];
        PluginState state;
        var source = new FakePlugin();
        source.Component.State = bytes;
        using (var plugin = new HostedPlugin(source))
            state = PluginState.From(plugin.Provider.Id, plugin.Component, plugin.Controller);

        Assert.That(state.Id, Is.EqualTo(FakePlugin.ClassId));
        Assert.That(state.Component, Is.EqualTo(bytes));
        // The fake's controller has no state of its own (getState answers kNotImplemented)
        Assert.That(state.Controller, Is.Empty);

        var target = new FakePlugin();
        target.Controller.ImplementsSetComponentState = true;
        using (var plugin = new HostedPlugin(target))
        {
            plugin.Component.setState(state.GetComponentStream());
            plugin.Controller.setComponentState(state.GetComponentStream());
        }

        Assert.That(target.Component.State, Is.EqualTo(bytes));
        Assert.That(target.Controller.ReceivedComponentState, Is.EqualTo(bytes));
    }

    [Test]
    public void Process_DeliversMidiNotesAndParameterChanges()
    {
        var fake = new FakePlugin();
        using var plugin = new HostedPlugin(fake);

        // As EffectHost: MIDI in, VST events out, collected in an EventList for the next process call
        ChannelMessage[] messages =
        [
            new(ChannelCommand.NoteOn, 2, 60, 100),
            new(ChannelCommand.NoteOn, 3, 60, 90),       // same key on another (MPE) channel
            new(ChannelCommand.PolyPressure, 2, 60, 64),
            new(ChannelCommand.NoteOn, 2, 60, 0),        // note on with velocity 0 is a note off
            new(ChannelCommand.NoteOff, 3, 60, 10),
        ];
        var events = new EventList();
        foreach (var m in messages)
        {
            Assert.That(EffectHost.TryTranslateToEvent(m, out var e), Is.True, $"{m.Command}");
            events.AddEvent(in e);
        }
        Assert.That(EffectHost.TryTranslateToEvent(new ChannelMessage(ChannelCommand.Controller, 2, 7, 100), out _), Is.False);

        var changes = new ParameterChanges();
        changes.AddParameterData(GainId, out _).addPoint(0, 0.75);

        var data = new ProcessData
        {
            ProcessMode = ProcessModes.Realtime,
            SymbolicSampleSize = SymbolicSampleSizes.Sample32,
            NumSamples = 64,
            InputEvents = events,
            InputParameterChanges = changes,
        };
        plugin.Processor.process(in data);

        var received = fake.Component.ReceivedEvents;
        Assert.That(received.Select(e => e.Type), Is.EqualTo(new[]
        {
            Event.EventTypes.kNoteOnEvent, Event.EventTypes.kNoteOnEvent, Event.EventTypes.kPolyPressureEvent,
            Event.EventTypes.kNoteOffEvent, Event.EventTypes.kNoteOffEvent,
        }));

        var noteOn = received[0].GetValue<NoteOnEvent>();
        Assert.That((noteOn.Channel, noteOn.Pitch, noteOn.NoteId), Is.EqualTo(((short)2, (short)60, 2 * 128 + 60)));
        Assert.That(noteOn.Velocity, Is.EqualTo((float)MidiConvert.Midi7BitToNormalized(100)));

        var otherChannel = received[1].GetValue<NoteOnEvent>();
        Assert.That((otherChannel.Channel, otherChannel.Pitch, otherChannel.NoteId), Is.EqualTo(((short)3, (short)60, 3 * 128 + 60)));

        var pressure = received[2].GetValue<PolyPressureEvent>();
        Assert.That((pressure.Channel, pressure.Pitch, pressure.NoteId), Is.EqualTo(((short)2, (short)60, 2 * 128 + 60)));
        Assert.That(pressure.Pressure, Is.EqualTo(0.5f));

        var noteOff = received[3].GetValue<NoteOffEvent>();
        Assert.That((noteOff.Channel, noteOff.Pitch, noteOff.NoteId), Is.EqualTo(((short)2, (short)60, 2 * 128 + 60)));
        Assert.That(noteOff.Velocity, Is.EqualTo(0f));

        var otherOff = received[4].GetValue<NoteOffEvent>();
        Assert.That((otherOff.Channel, otherOff.Pitch, otherOff.NoteId), Is.EqualTo(((short)3, (short)60, 3 * 128 + 60)));

        Assert.That(fake.Component.ReceivedParameterChanges, Is.EqualTo(new[] { (GainId, 0, 0.75) }));
    }

    [Test]
    public void SetComponentState_NotImplemented_IsTolerated()
    {
        var state = new PluginState(FakePlugin.ClassId, [9, 8, 7], ImmutableArray<byte>.Empty);
        var fake = new FakePlugin();
        using var plugin = new HostedPlugin(fake);

        // kNotImplemented arrives as NotImplementedException, which EffectHost ignores
        Assert.Throws<NotImplementedException>(() => plugin.Controller.setComponentState(state.GetComponentStream()));
        Assert.DoesNotThrow(() =>
        {
            plugin.Component.IgnoreNotImplementedException(c => c.setState(state.GetComponentStream()));
            plugin.Controller.IgnoreNotImplementedException(c => c.setComponentState(state.GetComponentStream()));
        });
        Assert.That(fake.Component.State, Is.EqualTo(new byte[] { 9, 8, 7 }));
    }
}
