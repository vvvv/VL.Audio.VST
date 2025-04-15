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
using VL.Audio.VST.Internal;
using VL.Core;
using VL.Core.Commands;
using VL.Core.CompilerServices;
using VL.Core.Import;
using VL.Core.PublicAPI;
using VL.Core.Reactive;
using VL.Lang.PublicAPI;
using VL.Lib.Collections;
using VL.Lib.Reactive;
using VL.Model;
using VST3;
using VST3.Hosting;

namespace VL.Audio.VST;

using IComponent = VST3.IComponent;
using PinAttribute = Core.Import.PinAttribute;

[GeneratedComClass]
[Smell(SymbolSmell.Advanced)]
[ProcessNodeFactory(typeof(EffectHostFactory))]
public partial class EffectHost : FactoryBasedVLNode, IHasCommands, IHasLearnMode, IComponentHandler, IComponentHandler2, IDisposable
{
    internal const string StateInputPinName = "State";
    internal const string WindowStatePinName = "Window State";
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
    private readonly IDisposable? nodeRegistration;

    private readonly EventList inputEventList = new();
    private readonly EventList outputEventList = new();
    private readonly SerialDisposable midiInputSubscription = new();
    private readonly BlockingCollection<Event> inputEventQueue = new(boundedCapacity: 1024);
    private readonly Subject<Event> outputEvents = new();

    private IChannel<WindowState>? windowStateChannel;
    private Dictionary<string, object>? currentParameters;
    private readonly IChannel<bool> learnMode = Channel.Create(false);

    private PluginState? state;
    private bool stateIsBeingSet;
    private WindowState? windowState;
    private IObservable<IMidiMessage>? midiInput;
    private readonly Dictionary<string, object> inputValues = new();
    private bool apply;
    private readonly IObservable<IMidiMessage> midiOutput;

    private ParameterInfo? byPassParameter;

    private readonly BlockingCollection<ParameterChanges> inputParameterChangesQueue = new(boundedCapacity: 1);
    private readonly BlockingCollection<ParameterChanges> outputParameterChangesQueue = new(boundedCapacity: 1);
    private ParameterChanges? accumulatedInputParameterChanges, accumulatedOutputParameterChanges;

    private readonly Dictionary<uint, ParameterInfo> parameters = new();
    private readonly Dictionary<uint, double> parameterValues = new();
    private readonly Dictionary<string, uint> parameterLookup = new();
    private readonly Dictionary<int, UnitInfo> units = new();

    private ImmutableArray<BusInfo> audioInputBusses, audioOutputBusses, eventInputBusses, eventOutputBusses;
    private readonly ProcessSetup processSetup;
    private readonly AudioOutput audioOutput;
    private IChannel<PluginState>? stateChannel;

    [Fragment]
    public EffectHost(NodeContext nodeContext) : this(nodeContext, EffectNodeInfo.Parse(nodeContext.PrivateData!))
    {
    }

    private EffectHost(NodeContext nodeContext, EffectNodeInfo info) : base(nodeContext)
    {
        this.nodeContext = nodeContext;
        this.info = info;
        this.logger = nodeContext.GetLogger();
        this.synchronizationContext = SynchronizationContext.Current;

        var modulePath = EffectHostFactory.ResolveModulePath(info.ModuleName, nodeContext.AppHost.NodeFactoryRegistry.Paths);
        if (modulePath is null)
            throw new FileNotFoundException($"Module {info.ModuleName} not found");

        module = Module.Create(modulePath);
        plugProvider = PlugProvider.Create(module.Factory, info.Id, s_context)!;
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
        processor.setProcessing(true);

        midiOutput = outputEvents.ObserveOn(Scheduler.Default).SelectMany(e => TryTranslateToMidi(in e, out var m) ? new[] { m } : []);

        ReloadParameters();

        controller?.setComponentHandler(this);

        nodeRegistration = IDevSession.Current?.RegisterNode(this);
    }

    IChannel<bool> IHasLearnMode.LearnMode => learnMode;

    private bool HasMainAudioIn => audioInputBusses.Length > 0 && audioInputBusses[0].BusType == BusTypes.kMain;
    private bool HasMainAudioOut => audioOutputBusses.Length > 0 && audioOutputBusses[0].BusType == BusTypes.kMain;
    private bool HasEventInput => eventInputBusses.Length > 0;
    private bool HasEventOutput => eventOutputBusses.Length > 0;

    void IDisposable.Dispose()
    {
        if (isDisposed)
            return;

        isDisposed = true;

        // Ensure we're not processing any audio currently
        lock (processingLock)
        {
            nodeRegistration?.Dispose();
            midiInputSubscription.Dispose();
            HideUI();
            audioOutput.Dispose();

            processor.setProcessing(false);
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

        var channel = stateChannel;
        if (channel is null)
            return solution;

        // Acknowledge the new state, we don't want to trigger a SetState on the controller in the next update
        var state = PluginState.From(plugProvider.Id, component, controller);
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

    [Fragment]
    public void Update(
        IChannel<PluginState> state, 
        IChannel<WindowState> windowState,
        IReadOnlyList<AudioSignal> audioIn,
        IObservable<IMidiMessage> midiIn,
        [Pin(PinGroupKind = PinGroupKind.Dictionary, PinGroupEditMode = PinGroupEditModes.RemoveOnly)] Dictionary<string, object> parameters,
        [DefaultValue(true)] bool apply,
        out IReadOnlyList<AudioSignal> audioOut,
        out IObservable<IMidiMessage> midiOut)
    {
        // TODO: Make thread safe!
        var aduioInputSignals = audioIn;
        Array.Resize(ref inputAudioSignals, aduioInputSignals.Count);
        for (int i = 0; i < inputAudioSignals.Length; i++)
            inputAudioSignals[i] = aduioInputSignals[i];

        stateChannel = state;
        windowStateChannel = windowState;

        if (Acknowledge(ref this.state, state.Value))
        {
            stateIsBeingSet = true;
            try
            {
                if (this.state != null && this.state.Id == plugProvider.Id)
                {
                    if (this.state.HasComponentData)
                    {
                        component.IgnoreNotImplementedException(c => c.setState(this.state.GetComponentStream()));
                        controller?.IgnoreNotImplementedException(c => c.setComponentState(this.state.GetComponentStream()));
                    }
                    if (this.state.HasControllerData)
                        controller?.IgnoreNotImplementedException(c => c.setState(this.state.GetControllerStream()));
                }
            }
            finally
            {
                stateIsBeingSet = false;
            }
        }

        if (Acknowledge(ref midiInput, midiIn))
        {
            midiInputSubscription.Disposable = null;
            midiInputSubscription.Disposable = midiInput?.Subscribe(HandleMidiMessage);
        }

        if (currentParameters is null)
            SyncPinGroup(parameters);

        // Check for changes on our input pins
        currentParameters = parameters;
        foreach (var (k, v) in parameters)
        {
            if (inputValues.TryGetValue(k, out var e) && Equals(e, v))
                continue;

            if (!parameterLookup.TryGetValue(k, out var id) || !this.parameters.TryGetValue(id, out var p))
                continue;

            inputValues[k] = v;
            SetParameter(id, p.Normalize(v));
        }

        if (Acknowledge(ref this.windowState, windowState.Value))
        {
            if (this.windowState is null || this.windowState.IsVisible)
                ShowUI(this.windowState?.Bounds, this.windowState?.Visibility.ToFormWindowState(), trackWindowState: true);
            else
                HideUI();
        }

        if (Acknowledge(ref this.apply, apply))
        {
            if (byPassParameter.HasValue)
                SetParameter(byPassParameter.Value.ID, !apply ? 1.0 : 0.0);
        }


        // Move upcoming changes to audio thread
        var inputChanges = accumulatedInputParameterChanges;
        if (inputChanges != null && inputParameterChangesQueue.TryAdd(inputChanges))
            accumulatedInputParameterChanges = null;

        // Move output changes from audio thread and notify UI
        if (outputParameterChangesQueue.TryTake(out var outputChanges))
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

        audioOut = audioOutput.Outputs;
        midiOut = midiOutput;
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
        // Load units first as we need them to compute pin name
        units.Clear();
        if (controller is IUnitInfo unitInfo)
        {
            foreach (var u in unitInfo.GetUnitInfos())
                units[u.Id] = u;
        }

        // Now that units are known, load the parameters
        parameters.Clear();
        parameterLookup.Clear();
        if (controller != null)
        {
            foreach (var p in controller.GetParameters())
            {
                if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsBypass))
                    byPassParameter = p;
                parameters[p.ID] = p;
                parameterLookup[GetParameterFullName(p)] = p.ID;
            }
        }

        // Invalidate parameter names and channels
        parameterNames = null;
        channels.Clear();

        // Invalidate the pin group
        currentParameters = null;
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
        if (parameterValues.TryGetValue(id, out var c) && c == normalizedValue)
            return;

        parameterValues[id] = normalizedValue;
        var inputChanges = accumulatedInputParameterChanges ??= ParameterChangesPool.Default.Get();
        var queue = inputChanges.AddParameterData(in id, out _);
        queue.addPoint(0, normalizedValue);

        controller?.setParamNormalized(id, normalizedValue);
        OnParameterChanged(id, normalizedValue);
    }

    void IComponentHandler.beginEdit(uint id)
    {
        logger.LogTrace("Begin edit for parameter {id}", id);

        editCount++;

        if (learnMode.Value)
            AddToPinGroup(id);
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

            // Save the parameter value to the pin as well (if it exists)
            if (solution != null && parameters.TryGetValue(id, out var p) && parameterValues.TryGetValue(id, out var normalizedValue))
            {
                var pinName = GetParameterFullName(in p);
                solution = SaveToPin(solution, pinName, p.GetValueAsObject(normalizedValue));
            }

            solution?.Confirm(updateKind);
        }
    }

    void IComponentHandler.performEdit(uint id, double valueNormalized)
    {
        SetParameter(id, valueNormalized);
    }

    void IComponentHandler.restartComponent(RestartFlags flags)
    {
        logger.LogTrace("Restarting component with flags {flags}", flags);

        if (flags.HasFlag(RestartFlags.kParamTitlesChanged))
            ReloadParameters();

        if (flags.HasFlag(RestartFlags.kParamValuesChanged))
        {
            // Some plugins (like Kontakt8) do not report the correct state yet - post poning the call seems to do the trick
            if (SynchronizationContext.Current != null)
                SynchronizationContext.Current.Post(_ => SavePluginState(), null);
            else
                SavePluginState();
        }
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

    IEnumerable<(string Name, ICommand Command)> IHasCommands.Commands
    {
        get
        {
            yield return ("Show UI", Command.Create(
                () =>
                {
                    var formWindowState = System.Windows.Forms.FormWindowState.Normal;
                    if (windowState != null && windowState.Visibility >= WindowVisibility.Normal)
                        formWindowState = windowState.Visibility.ToFormWindowState();
                    ShowUI(windowState?.Bounds, formWindowState, trackWindowState: true);
                })
                .ExecuteOn(nodeContext.AppHost.SynchronizationContext));
        }
    }

    private ISolution? SaveToChannelOrPin<T>(IChannel<T> channel, ISolution? solution, string pinName, T value)
    {
        // Only write changes to the channel. Avoids document marked as dirty on open.
        if (!Equals(channel.Value, value))
        {
            channel.Value = value;

            if (solution != null)
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

    private void AddToPinGroup(uint id)
    {
        if (controller is null || !parameters.TryGetValue(id, out var parameter))
            return;

        var solution = IDevSession.Current?.CurrentSolution;
        if (solution is null)
            return;

        var typeRegistry = AppHost.Global.TypeRegistry;
        var current = currentParameters ?? new();
        var builder = GetPinGroupBuilder(solution);
        foreach (var p in controller!.GetParameters())
        {
            var pinName = GetParameterFullName(in p);
            if (current.ContainsKey(pinName) || id == p.ID)
            {
                builder.Add(pinName, typeRegistry.GetTypeInfo(p.GetPinType()).Name);
                this.type = null;
            }
        }

        var s = builder.Commit();
        if (s != solution)
        {
            var pinName = GetParameterFullName(in parameter);
            var normalizedValue = controller.getParamNormalized(parameter.ID);
            s = SaveToPin(s, pinName, parameter.GetValueAsObject(normalizedValue));
            s.Confirm();
        }
    }

    private void SyncPinGroup(Dictionary<string, object> parameters)
    {
        var solution = IDevSession.Current?.CurrentSolution;
        if (solution is null)
            return;

        var typeRegistry = AppHost.Global.TypeRegistry;
        var builder = GetPinGroupBuilder(solution);
        foreach (var p in controller!.GetParameters())
        {
            var pinName = GetParameterFullName(in p);
            if (parameters.ContainsKey(pinName))
                builder.Add(pinName, typeRegistry.GetTypeInfo(p.GetPinType()).Name);
        }
        builder.Commit().Confirm(SolutionUpdateKind.TweakLast);
    }

    private PinGroupBuilder GetPinGroupBuilder(ISolution solution)
    {
        var nodeId = nodeContext.Path.Stack.Peek();
        var builder = solution.ModifyPinGroup(nodeId, "Parameters", isInput: true);
        return builder;
    }
}
