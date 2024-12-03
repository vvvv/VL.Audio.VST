using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sanford.Multimedia.Midi;
using Stride.Core.Mathematics;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.Marshalling;
using VL.Core;
using VL.Core.Reactive;
using VL.Lang.PublicAPI;
using VL.Lib.Collections;
using VL.Lib.IO.Midi;
using VL.Lib.Reactive;
using VST3;
using VST3.Hosting;

namespace VL.Audio.VST;

using StatePin = Pin<IChannel<PluginState>>;
using AudioPin = Pin<IReadOnlyList<AudioSignal>>;
using IComponent = VST3.IComponent;

[GeneratedComClass]
public partial class EffectHost : FactoryBasedVLNode, IVLNode, IComponentHandler, IComponentHandler2, IDisposable
{
    public const string StateInputPinName = "State";
    public const string BoundsInputPinName = "Bounds";

    private static readonly ParameterChanges s_noChanges = new();

    private static readonly HostApp s_context = new HostApp([typeof(IMidiMapping), typeof(IMidiLearn)]);

    private readonly object processingLock = new();
    private bool isDisposed;
    private int editCount;

    private readonly NodeContext nodeContext;
    private readonly EffectNodeInfo info;
    private readonly ILogger logger;
    private readonly Module module;
    private readonly PlugProvider plugProvider;
    private readonly IComponent component;
    private readonly IAudioProcessor processor;
    private readonly IEditController? controller;
    private readonly SynchronizationContext? synchronizationContext;

    private readonly EventList inputEventList = new();
    private readonly EventList outputEventList = new();
    private readonly SerialDisposable midiInputSubscription = new();
    private readonly BlockingCollection<Event> inputEventQueue = new(boundedCapacity: 1024);
    private readonly Subject<Event> outputEvents = new();

    private readonly Pin<IChannel<RectangleF>> boundsPin;
    private readonly Pin<IObservable<IMidiMessage>> midiInputPin, midiOutputPin;
    //private readonly Pin<string> channelPrefixPin;
    private readonly Pin<bool> showUiPin;
    private readonly Pin<bool> applyPin;

    private PluginState? state;
    private bool stateIsBeingSet;
    private RectangleF bounds;
    private AudioPin audioInputPin, audioOutputPin;
    private IObservable<IMidiMessage>? midiInput;
    private string? channelPrefix;
    private bool showUI;
    private bool apply;

    private ParameterInfo? byPassParameter;

    private ParameterChanges? upcomingChanges;
    private ParameterChanges? committedChanges;
    private ParameterChanges? pendingOutputChanges, acknowledgedOutputChanges;

    private readonly Dictionary<uint, (ParameterInfo parameter, IChannel channel)> channels = new();
    private readonly Dictionary<uint, ParameterInfo> parameters = new();
    private readonly Dictionary<int, UnitInfo> units = new();

    private ImmutableArray<BusInfo> audioInputBusses, audioOutputBusses, eventInputBusses, eventOutputBusses;
    private float[] leftInput = [];
    private float[] rightInput = [];
    private readonly ProcessSetup processSetup;
    private readonly AudioOutput audioOutput;
    private readonly StatePin statePin;

    internal EffectHost(NodeContext nodeContext, EffectNodeInfo info) : base(nodeContext)
    {
        this.nodeContext = nodeContext;
        this.info = info;
        this.logger = nodeContext.GetLogger();
        this.synchronizationContext = SynchronizationContext.Current;

        module = Module.Create(info.ModulePath);
        plugProvider = PlugProvider.Create(module.Factory, info.ClassInfo, s_context)!;
        component = plugProvider.Component;
        processor = (IAudioProcessor)component;
        controller = plugProvider.Controller;

        processor.setupProcessing(
            processSetup = new ProcessSetup()
            {
                ProcessMode = ProcessModes.Realtime,
                SymbolicSampleSize = SymbolicSampleSizes.Sample32,
                MaxSamplesPerBlock = Math.Max(AudioService.Engine.Settings.BufferSize, 4096),
                SampleRate = AudioService.Engine.Settings.SampleRate
            });

        audioInputBusses = component.GetBusInfos(MediaTypes.kAudio, BusDirections.kInput).ToImmutableArray();
        audioOutputBusses = component.GetBusInfos(MediaTypes.kAudio, BusDirections.kOutput).ToImmutableArray();
        eventInputBusses = component.GetBusInfos(MediaTypes.kEvent, BusDirections.kInput).ToImmutableArray();
        eventOutputBusses = component.GetBusInfos(MediaTypes.kEvent, BusDirections.kOutput).ToImmutableArray();

        // Activate main buses
        audioOutput = new AudioOutput(this);
        if (HasMainAudioIn)
            component.activateBus(MediaTypes.kAudio, BusDirections.kInput, 0, true);
        if (HasMainAudioOut)
        {
            component.activateBus(MediaTypes.kAudio, BusDirections.kOutput, 0, true);
            audioOutput.SetOutputCount(audioOutputBusses[0].ChannelCount);
        }
        if (HasEventInput)
            component.activateBus(MediaTypes.kEvent, BusDirections.kInput, 0, true);
        if (HasEventOutput)
            component.activateBus(MediaTypes.kEvent, BusDirections.kOutput, 0, true);

        component.setActive(true);
        processor.SetProcessing_IgnoreNotImplementedException(true);

        Inputs = new IVLPin[info.NodeDescription.Inputs.Count];
        Outputs = new IVLPin[info.NodeDescription.Outputs.Count];

        var i = 0; var o = 0;

        Inputs[i] = statePin = new StatePin();
        i++;
        Inputs[i++] = boundsPin = new Pin<IChannel<RectangleF>>();
        Inputs[i++] = audioInputPin = new AudioPin();
        Inputs[i++] = midiInputPin = new Pin<IObservable<IMidiMessage>>();
        //Inputs[i++] = new ParametersInput(this);
        //Inputs[i++] = channelPrefixPin = new Pin<string>();
        Inputs[i++] = showUiPin = new Pin<bool>();
        Inputs[i++] = applyPin = new Pin<bool>();

        //Outputs[o++] = new Pin<EffectHost>() { Value = this };
        Outputs[o++] = audioOutputPin = new AudioPin();
        Outputs[o++] = midiOutputPin = new Pin<IObservable<IMidiMessage>>() 
        { 
            Value = outputEvents.ObserveOn(Scheduler.Default).SelectMany(e => TryTranslateToMidi(in e, out var m) ? new[] { m } : [])
        };

        if (controller != null)
        {
            controller.setComponentHandler(this);

            ReloadParameters();
        }
    }

    public IVLNodeDescription NodeDescription => info.NodeDescription;

    public IVLPin[] Inputs { get; }

    public IVLPin[] Outputs { get; }

    private bool HasMainAudioIn => audioInputBusses.Length > 0 && audioInputBusses[0].BusType == BusTypes.kMain;
    private bool HasMainAudioOut => audioOutputBusses.Length > 0 && audioOutputBusses[0].BusType == BusTypes.kMain;
    private bool HasEventInput => eventInputBusses.Length > 0;
    private bool HasEventOutput => eventOutputBusses.Length > 0;

    public void Dispose()
    {
        if (isDisposed)
            return;

        isDisposed = true;

        // Ensure we're not processing any audio currently
        lock (processingLock)
        {
            midiInputSubscription.Dispose();
            HideEditor();
            audioOutput.Dispose();

            processor.SetProcessing_IgnoreNotImplementedException(false);
            component.setActive(false);

            plugProvider.Dispose();
            module.Dispose();
        }
    }

    private void SavePluginState()
    {
        if (stateIsBeingSet)
            return;

        var channel = statePin.Value;
        if (channel is null)
            return;

        // Acknowledge the new state, we don't want to trigger a SetState on the controller in the next update
        var state = PluginState.From(plugProvider.ClassInfo.ID, component, controller);
        if (Acknowledge(ref this.state, state))
            SaveToChannelOrPin(channel, StateInputPinName, state);
    }

    private static bool Acknowledge<T>(ref T current, T value)
    {
        if (!EqualityComparer<T>.Default.Equals(current, value))
        {
            current = value;
            return true;
        }
        return false;
    }

    public void Update()
    {
        // TODO: Make thread safe!
        var aduioInputSignals = audioInputPin.Value ?? Array.Empty<AudioSignal>();
        Array.Resize(ref inputAudioSignals, aduioInputSignals.Count);
        for (int i = 0; i < inputAudioSignals.Length; i++)
            inputAudioSignals[i] = aduioInputSignals[i];

        if (Acknowledge(ref state, statePin.Value?.Value))
        {
            stateIsBeingSet = true;
            try
            {
                if (state != null && state.Id == plugProvider.ClassInfo.ID)
                {
                    if (state.HasComponentData)
                    {
                        component.IgnoreNotImplementedException(c => c.setState(state.GetComponentStream()));
                        controller?.IgnoreNotImplementedException(c => c.setComponentState(state.GetComponentStream()));
                    }
                    if (state.HasControllerData)
                        controller?.IgnoreNotImplementedException(c => c.setState(state.GetControllerStream()));
                }
            }
            finally
            {
                stateIsBeingSet = false;
            }
        }

        if (Acknowledge(ref midiInput, midiInputPin.Value))
        {
            midiInputSubscription.Disposable = null;
            midiInputSubscription.Disposable = midiInput?.Subscribe(HandleMidiMessage);
        }

        //if (Acknowledge(ref channelPrefix, channelPrefixPin.Value))
        //{
        //    if (!string.IsNullOrEmpty(channelPrefix))
        //        LoadChannels(channelPrefix);
        //}

        if (Acknowledge(ref showUI, showUiPin.Value))
        {
            if (showUI)
                ShowEditor();
            else
                HideEditor();
        }

        if (Acknowledge(ref bounds, boundsPin.Value?.Value ?? RectangleF.Empty))
        {
            if (!bounds.IsEmpty)
                SetWindowBounds(bounds);
        }

        if (Acknowledge(ref apply, applyPin.Value))
        {
            if (byPassParameter.HasValue)
                SetParameter(byPassParameter.Value.ID, !apply ? 1.0 : 0.0);
        }


        // Move upcoming changes to audio thread and notify UI
        var inputChanges = this.upcomingChanges;
        if (inputChanges != null && this.committedChanges is null)
        {
            upcomingChanges = null;

            for (int i = 0; i < inputChanges.GetParameterCount(); i++)
            {
                var queue = inputChanges.GetParameterData(i);
                if (queue is null)
                    continue;
                queue.getPoint(0, out _, out var value);

                var id = queue.getParameterId();
                controller?.setParamNormalized(id, value);
                OnParameterChanged(id, value);
            }

            Interlocked.Exchange(ref this.committedChanges, inputChanges);
        }

        // Move output changes from audio thread and notify UI
        var outputChanges = Interlocked.Exchange(ref this.acknowledgedOutputChanges, null);
        if (outputChanges != null)
        {
            for (int i = 0; i < outputChanges.GetParameterCount(); i++)
            {
                var queue = outputChanges.GetParameterData(i);
                if (queue is null)
                    continue;
                queue.getPoint(0, out _, out var value);

                var id = queue.getParameterId();
                controller?.setParamNormalized(id, value);
                if (channels.TryGetValue(id, out var x))
                    x.channel.Object = x.parameter.GetValueAsObject(value);
                OnParameterChanged(id, value);
            }
            ParameterChangesPool.Default.Return(outputChanges);
        }

        audioOutputPin.Value = audioOutput.Outputs;
    }

    private readonly Subject<(ParameterInfo parameter, double normalizedValue)> ParameterChanged = new();

    private void OnParameterChanged(uint id, double normalizedValue)
    {
        if (parameters.TryGetValue(id, out var parameter))
        {
            ParameterChanged.OnNext((parameter, normalizedValue));
        }
    }

    private void ReloadParameters()
    {
        if (controller is null) return;

        parameters.Clear();
        foreach (var p in controller.GetParameters())
        {
            if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsBypass))
                byPassParameter = p;
            parameters[p.ID] = p;
        }

        units.Clear();
        if (controller is IUnitInfo unitInfo)
        {
            foreach (var u in unitInfo.GetUnitInfos())
                units[u.Id] = u;
        }
    }

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

    private void LoadChannels(string prefix)
    {
        channels.Clear();

        if (controller is null)
            return;

        var channelHub = nodeContext.AppHost.Services.GetService<IChannelHub>();
        if (channelHub is null)
            return;

        var unitController = controller as IUnitInfo;
        var units = unitController?.GetUnitInfos().ToDictionary(u => u.Id);

        channelHub.BatchUpdate(channelHub =>
        {
            foreach (var p in controller.GetParameters())
            {
                if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsBypass))
                    continue;
                if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsHidden))
                    continue;
                if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsList))
                    continue;
                if (!p.Flags.HasFlag(ParameterInfo.ParameterFlags.kCanAutomate))
                    continue;

                var unitName = units != null ? GetUnitFullName(units, p.UnitId) : null;
                var localPrefix = string.IsNullOrEmpty(unitName) ? prefix : $"{prefix}.{unitName}";
                var key = $"{localPrefix}.{p.Title}";
                if (channelHub.TryGetChannel(key) != null)
                    continue;

                var channel = channelHub.TryAddChannel(key, p.GetPinType());
                if (channel is null) 
                    continue;

                var attributes = channel.Attributes().Value;
                if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsReadOnly))
                    attributes = attributes.Add(new System.ComponentModel.ReadOnlyAttribute(isReadOnly: true));
                attributes = attributes.Add(new System.ComponentModel.DefaultValueAttribute(p.GetDefaultValue()));
                channel.Attributes().Value = attributes;
                channel.Value = p.GetCurrentValue(controller);

                if (!p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsReadOnly))
                {
                    channel.Subscribe(v =>
                    {
                        var normalized = p.Normalize(v);
                        SetParameter(p.ID, normalized);
                    });
                }

                channels.Add(p.ID, (p, channel));
            }
        });

        string GetUnitFullName(Dictionary<int, UnitInfo> units, int infoId)
        {
            if (infoId == Constants.kRootUnitId)
                return string.Empty;

            var info = units[infoId];
            var parentName = GetUnitFullName(units, info.ParentUnitId);
            if (!string.IsNullOrEmpty(parentName))
                return $"{parentName}.{info.Name}";

            return info.Name;
        }
    }

    private void SetParameter(uint id, double normalizedValue)
    {
        var inputChanges = this.upcomingChanges ??= ParameterChangesPool.Default.Get();
        var queue = inputChanges.AddParameterData(in id, out _);
        queue.addPoint(0, normalizedValue);
    }

    void IComponentHandler.beginEdit(uint id)
    {
        logger.LogTrace("Begin edit for parameter {id}", id);

        editCount++;
    }

    void IComponentHandler.endEdit(uint id)
    {
        logger.LogTrace("End edit for parameter {id}", id);

        editCount--;
        if (editCount == 0)
            SavePluginState();
    }

    void IComponentHandler.performEdit(uint id, double valueNormalized)
    {
        if (channels.TryGetValue(id, out var x))
        {
            var (parameter, channel) = x;
            channel.Object = parameter.GetValueAsObject(valueNormalized);
        }
        else
        {
            SetParameter(id, valueNormalized);
        }
    }

    void IComponentHandler.restartComponent(RestartFlags flags)
    {
        logger.LogTrace("Restarting component with flags {flags}", flags);

        if (flags.HasFlag(RestartFlags.kParamTitlesChanged))
            ReloadParameters();

        if (flags.HasFlag(RestartFlags.kParamValuesChanged))
            SavePluginState();
    }

    void IComponentHandler2.setDirty(bool state)
    {
        logger.LogTrace("Setting dirty state to {state}", state);

        if (true)
            SavePluginState();
    }

    void IComponentHandler2.requestOpenEditor(string name)
    {
        logger.LogTrace("Requesting editor {name}", name);
    }

    void IComponentHandler2.startGroupEdit()
    {
        logger.LogTrace("Starting group edit");
    }

    void IComponentHandler2.finishGroupEdit()
    {
        logger.LogTrace("Finishing group edit");
    }

    private void SaveToChannelOrPin<T>(IChannel<T> channel, string pinName, T value)
    {
        // Only write changes to the channel. Avoids document marked as dirty on open.
        if (Equals(channel.Value, value))
            return;

        channel.Value = value;

        if (channel.IsSystemGenerated())
            SaveToPin(pinName, value);
    }

    private void SaveToPin<T>(string pinName, T value)
    {
        var solution = IDevSession.Current?.CurrentSolution
            .SetPinValue(nodeContext.Path.Stack, pinName, value);
        solution?.Confirm(Model.SolutionUpdateKind.DontCompile | Model.SolutionUpdateKind.TweakLast);
    }

    sealed class ParametersInput : IVLPin<IReadOnlyDictionary<string, float>>
    {
        private readonly EffectHost host;
        private readonly Dictionary<string, float> currentValues = new();
        private IReadOnlyDictionary<string, float>? value;

        public ParametersInput(EffectHost host)
        {
            this.host = host;
        }

        public IReadOnlyDictionary<string, float>? Value
        {
            get => value;
            set
            {
                if (value is null)
                    return;

                if (host.controller is null)
                    return;

                foreach (var (k, v) in value)
                {
                    if (currentValues.TryGetValue(k, out var c) && c == v)
                        continue;

                    var parameter = host.controller.GetParameters().FirstOrDefault(p => string.Equals(p.Title, k, StringComparison.OrdinalIgnoreCase));
                    if (parameter.Title is null)
                        continue;

                    host.SetParameter(parameter.ID, v);
                }

                currentValues.Clear();
                foreach (var (k, v) in value)
                    currentValues.Add(k, v);
            }
        }

        object? IVLPin.Value { get => Value; set => Value = (IReadOnlyDictionary<string, float>?)value; }
    }
}
