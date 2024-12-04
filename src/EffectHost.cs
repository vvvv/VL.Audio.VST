using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sanford.Multimedia.Midi;
using Stride.Core.Mathematics;
using System.Collections.Concurrent;
using System.Collections.Immutable;
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
    private const Model.SolutionUpdateKind JustWriteToThePin = Model.SolutionUpdateKind.Default & ~Model.SolutionUpdateKind.AffectCompilation & ~Model.SolutionUpdateKind.AddToHistory;

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
    private readonly Pin<Dictionary<string, object>> parametersPin;
    private readonly Pin<bool> showUiPin;
    private readonly Pin<bool> applyPin;
    private readonly IChannel<bool> learnMode = Channel.Create(false);

    private PluginState? state;
    private bool stateIsBeingSet;
    private RectangleF bounds;
    private AudioPin audioInputPin, audioOutputPin;
    private IObservable<IMidiMessage>? midiInput;
    private readonly Dictionary<string, object> parameterValues = new();
    private string? channelPrefix;
    private bool showUI;
    private bool apply;

    private ParameterInfo? byPassParameter;

    private ParameterChanges? upcomingChanges;
    private ParameterChanges? committedChanges;
    private ParameterChanges? pendingOutputChanges, acknowledgedOutputChanges;

    private readonly Dictionary<uint, ParameterInfo> parameters = new();
    private readonly Dictionary<uint, double> edits = new();
    private ILookup<string, ParameterInfo> parameterLookup;
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
        Inputs[i++] = parametersPin = new Pin<Dictionary<string, object>>();
        Inputs[i++] = showUiPin = new Pin<bool>();
        Inputs[i++] = applyPin = new Pin<bool>();

        Outputs[o++] = new Pin<EffectHost>() { Value = this };
        Outputs[o++] = audioOutputPin = new AudioPin();
        Outputs[o++] = midiOutputPin = new Pin<IObservable<IMidiMessage>>() 
        { 
            Value = outputEvents.ObserveOn(Scheduler.Default).SelectMany(e => TryTranslateToMidi(in e, out var m) ? new[] { m } : [])
        };

        ReloadParameters();

        controller?.setComponentHandler(this);
    }

    public IVLNodeDescription NodeDescription => info.NodeDescription;

    public IVLPin[] Inputs { get; }

    public IVLPin[] Outputs { get; }

    public IChannel<bool>? LearnMode => learnMode;

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
        SavePluginState(IDevSession.Current?.CurrentSolution)?.Confirm(JustWriteToThePin);
    }

    private ISolution? SavePluginState(ISolution? solution)
    {
        if (stateIsBeingSet)
            return solution;

        var channel = statePin.Value;
        if (channel is null)
            return solution;

        // Acknowledge the new state, we don't want to trigger a SetState on the controller in the next update
        var state = PluginState.From(plugProvider.ClassInfo.ID, component, controller);
        if (Acknowledge(ref this.state, state))
            return SaveToChannelOrPin(channel, solution, StateInputPinName, state);

        return solution;
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

        // Collect changed input parameters
        if (parametersPin.Value != null && controller != null)
        {
            foreach (var (k, v) in parametersPin.Value)
            {
                if (parameterValues.TryGetValue(k, out var c) && Equals(c, v))
                    continue;

                parameterValues[k] = v;

                foreach (var p in parameterLookup[k])
                    SetParameter(p.ID, p.Normalize(v));
            }
        }

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

    [MemberNotNull(nameof(parameterLookup))]
    private void ReloadParameters()
    {
        parameters.Clear();
        if (controller != null)
        {
            foreach (var p in controller.GetParameters())
            {
                if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsBypass))
                    byPassParameter = p;
                parameters[p.ID] = p;
            }
        }

        units.Clear();
        if (controller is IUnitInfo unitInfo)
        {
            foreach (var u in unitInfo.GetUnitInfos())
                units[u.Id] = u;
        }
        parameterLookup = parameters.Values.ToLookup(p => GetParameterFullName(in p));
    }

    private string GetParameterFullName(in ParameterInfo p, string separator = " ")
    {
        var unitName = GetUnitFullName(p.UnitId, separator);
        if (string.IsNullOrEmpty(unitName))
            return p.Title;
        return $"{unitName}{separator}{p.Title}";
    }

    private string GetUnitFullName(int unitId, string separator = " ")
    {
        if (unitId == Constants.kRootUnitId || !units.TryGetValue(unitId, out var info))
            return string.Empty;

        var parentName = GetUnitFullName(info.ParentUnitId);
        if (!string.IsNullOrEmpty(parentName))
            return $"{parentName}{separator}{info.Name}";
        return info.Name;
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
        {
            var updateKind = JustWriteToThePin;
            var solution = IDevSession.Current?.CurrentSolution;
            solution = SavePluginState(solution);

            if (parameters.TryGetValue(id, out var p))
            {
                var isNew = false;

                // In learn mode add the parameter to the pin group
                if (learnMode.Value && solution != null)
                {
                    var s = ModifyParametersPinGroup(solution, p);
                    if (s != solution)
                    {
                        updateKind = Model.SolutionUpdateKind.Default;
                        solution = s;
                        isNew = true;
                    }
                }

                // Save the parameter value to the pin (if it exists)
                if (solution != null && edits.TryGetValue(id, out var normalizedValue))
                {
                    var pinName = GetParameterFullName(in p);
                    if (isNew || parameterValues.ContainsKey(pinName))
                        solution = SaveToPin(solution, pinName, p.GetValueAsObject(normalizedValue));
                }
            }

            solution?.Confirm(updateKind);
        }
    }

    void IComponentHandler.performEdit(uint id, double valueNormalized)
    {
        edits[id] = valueNormalized;
        SetParameter(id, valueNormalized);
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

    private ISolution? SaveToChannelOrPin<T>(IChannel<T> channel, ISolution? solution, string pinName, T value)
    {
        // Only write changes to the channel. Avoids document marked as dirty on open.
        if (!Equals(channel.Value, value))
        {
            channel.Value = value;

            if (channel.IsSystemGenerated() && solution != null)
            {
                return SaveToPin(solution, pinName, value);
            }
        }
        return solution;
    }

    private ISolution SaveToPin<T>(ISolution solution, string pinName, T value)
    {
        return solution.SetPinValue(nodeContext.Path.Stack, pinName, value);
    }

    private ISolution? ModifyParametersPinGroup(ISolution solution, in ParameterInfo parameter)
    {
        var typeRegistry = AppHost.Global.TypeRegistry;
        var current = parametersPin.Value ?? new();
        var nodeId = nodeContext.Path.Stack.Peek();
        var builder = solution.ModifyPinGroup(nodeId, "Parameters", isInput: true);
        foreach (var p in controller!.GetParameters())
        {
            var pinName = GetParameterFullName(in p);
            if (current.ContainsKey(pinName) || parameter.ID == p.ID)
                builder.Add(pinName, typeRegistry.GetTypeInfo(p.GetPinType()).Name);
        }
        return builder.Commit();
    }
}
