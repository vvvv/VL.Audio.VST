using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Sanford.Multimedia.Midi;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reactive.Disposables;
using System.Runtime.InteropServices.Marshalling;
using System.Windows.Forms;
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

sealed class ParameterChangesPool : DefaultObjectPool<ParameterChanges>
{
    public static readonly ParameterChangesPool Default = new();

    public ParameterChangesPool() : base(new Policy())
    {
    }

    sealed class Policy : PooledObjectPolicy<ParameterChanges>
    {
        public override ParameterChanges Create() => new ParameterChanges();

        public override bool Return(ParameterChanges obj)
        {
            obj.Clear();
            return true;
        }
    }
}

[GeneratedComClass]
internal partial class EffectHost : FactoryBasedVLNode, IVLNode, IComponentHandler, IDisposable 
{
    public const string StateInputPinName = "State";

    private static readonly ParameterChanges s_noChanges = new();

    private readonly ManualResetEventSlim processingEvent = new(initialState: true);
    private bool isDisposed;

    private readonly NodeContext nodeContext;
    private readonly ILogger logger;
    private readonly PlugProvider plugProvider;
    private readonly IComponent component;
    private readonly IAudioProcessor processor;
    private readonly IEditController? controller;
    private readonly SynchronizationContext? synchronizationContext;

    private readonly EventList inputEvents = new();
    private readonly EventList outputEvents = new();
    private readonly SerialDisposable midiInputSubscription = new();
    private readonly BlockingCollection<Event> inputEventQueue = new(boundedCapacity: 1024);

    private readonly Pin<IObservable<IMidiMessage>> midiInputPin;
    private readonly Pin<string> channelPrefixPin;
    private readonly Pin<bool> showUiPin;
    private readonly Pin<bool> byPassPin;

    private PluginState? state;
    private IObservable<IMidiMessage>? midiInput;
    private string? channelPrefix;
    private bool showUI;
    private bool byPass;

    private readonly ParameterInfo? byPassParameter;

    private ParameterChanges? upcomingChanges;
    private ParameterChanges? committedChanges;
    private ParameterChanges? pendingOutputChanges, acknowledgedOutputChanges;

    private readonly Dictionary<uint, (ParameterInfo parameter, IChannel channel)> channels = new();

    private ImmutableArray<BusInfo> audioInputBusses, audioOutputBusses, eventInputBusses, eventOutputBusses;
    private float[] leftInput = [];
    private float[] rightInput = [];

    private readonly BufferCallerSignal outputSignal;
    private readonly ProcessContext processContext;
    private readonly ProcessSetup processSetup;

    private Form? window;

    public EffectHost(NodeContext nodeContext, IVLNodeDescription nodeDescription, PluginFactory factory, ClassInfo info, IHostApplication hostApplication) : base(nodeContext)
    {
        this.nodeContext = nodeContext;
        this.logger = nodeContext.GetLogger();
        this.synchronizationContext = SynchronizationContext.Current;

        NodeDescription = nodeDescription;

        plugProvider = PlugProvider.Create(factory, info, hostApplication)!;
        component = plugProvider.Component;
        processor = (IAudioProcessor)component;
        controller = plugProvider.Controller;

        outputSignal = new BufferCallerSignal()
        {
            PerBuffer = Process
        };

        processContext = new ProcessContext()
        {

        };



        processor.setupProcessing(
            processSetup = new ProcessSetup()
            {
                ProcessMode = ProcessModes.Realtime,
                SymbolicSampleSize = Utils.GetSymbolicSampleSizes(outputSignal.WaveFormat),
                MaxSamplesPerBlock = Math.Max(AudioService.Engine.Settings.BufferSize, 4096),
                SampleRate = AudioService.Engine.Settings.SampleRate
            });

        audioInputBusses = component.GetBusInfos(MediaTypes.kAudio, BusDirections.kInput).ToImmutableArray();
        audioOutputBusses = component.GetBusInfos(MediaTypes.kAudio, BusDirections.kOutput).ToImmutableArray();
        eventInputBusses = component.GetBusInfos(MediaTypes.kEvent, BusDirections.kInput).ToImmutableArray();
        eventOutputBusses = component.GetBusInfos(MediaTypes.kEvent, BusDirections.kOutput).ToImmutableArray();

        // Activate main buses
        if (HasStereoInput)
            component.activateBus(MediaTypes.kAudio, BusDirections.kInput, 0, true);
        if (HasStereoOutput)
            component.activateBus(MediaTypes.kAudio, BusDirections.kOutput, 0, true);
        if (HasEventInput)
            component.activateBus(MediaTypes.kEvent, BusDirections.kInput, 0, true);

        component.setActive(true);
        processor.SetProcessing_IgnoreNotImplementedException(true);

        Inputs = new IVLPin[nodeDescription.Inputs.Count];
        Outputs = new IVLPin[nodeDescription.Outputs.Count];

        var i = 0; var o = 0;

        Inputs[i] = new StatePin();
        i++;

        Inputs[i++] = new AudioIn(outputSignal);
        Inputs[i++] = midiInputPin = new Pin<IObservable<IMidiMessage>>();
        Inputs[i++] = new ParametersInput(this);
        Inputs[i++] = channelPrefixPin = new Pin<string>();
        Inputs[i++] = showUiPin = new Pin<bool>();
        Inputs[i++] = byPassPin = new Pin<bool>();

        Outputs[o++] = new AudioOut(outputSignal);

        if (controller != null)
        {
            controller.setComponentHandler(this);

            foreach (var p in controller.GetParameters())
            {
                if (p.Flags.HasFlag(ParameterInfo.ParameterFlags.kIsBypass))
                    byPassParameter = p;
            }
        }
    }

    public IVLNodeDescription NodeDescription { get; }

    public IVLPin[] Inputs { get; }

    public IVLPin[] Outputs { get; }

    private bool HasStereoInput => audioInputBusses.Length > 0 && audioInputBusses[0].BusType == BusTypes.kMain && audioInputBusses[0].ChannelCount == 2;
    private bool HasStereoOutput => audioOutputBusses.Length > 0 && audioOutputBusses[0].BusType == BusTypes.kMain && audioOutputBusses[0].ChannelCount == 2;
    private bool HasEventInput => eventInputBusses.Length > 0;
    private bool HasEventOutput => eventOutputBusses.Length > 0;

    public void Dispose()
    {
        if (isDisposed)
            return;

        isDisposed = true;
        // Ensure we're not processing any audio currently
        processingEvent.Wait();

        midiInputSubscription.Dispose();
        window?.Dispose();
        outputSignal.Dispose();

        var state = PluginState.From(plugProvider.ClassInfo.ID, component, controller);
        var statePin = (StatePin)Inputs[0];
        var channel = statePin.Value;
        if (channel.IsValid())
            channel.Value = state;
        else
            SaveToPin(StateInputPinName, state);

        processor.SetProcessing_IgnoreNotImplementedException(false);
        component.setActive(false);

        plugProvider.Dispose();
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
        if (Acknowledge(ref state, ((StatePin)Inputs[0]).Value?.Value))
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

        if (Acknowledge(ref midiInput, midiInputPin.Value))
        {
            midiInputSubscription.Disposable = null;
            midiInputSubscription.Disposable = midiInput?.Subscribe(HandleMidiMessage);
        }

        if (Acknowledge(ref channelPrefix, channelPrefixPin.Value))
        {
            if (!string.IsNullOrEmpty(channelPrefix))
                LoadChannels(channelPrefix);
        }

        if (Acknowledge(ref showUI, showUiPin.Value))
        {
            if (showUI)
                ShowEditor();
            else
                HideEditor();
        }

        if (Acknowledge(ref byPass, byPassPin.Value))
        {
            if (byPassParameter.HasValue)
                SetParameter(byPassParameter.Value.ID, byPass ? 1.0 : 0.0);
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
                controller?.setParamNormalized(queue.getParameterId(), value);
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
                controller?.setParamNormalized(queue.getParameterId(), value);
                if (channels.TryGetValue(queue.getParameterId(), out var x))
                    x.channel.Object = x.parameter.GetValueAsObject(value);
            }
            ParameterChangesPool.Default.Return(outputChanges);
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

    private void ShowEditor()
    {
        if (controller is null)
            return;

        if (window is null || window.IsDisposed)
        {
            var view = controller.createView("editor");
            if (view is null || view.isPlatformTypeSupported("HWND") != 0)
                return;

            window = new Form();

            // High DPI support
            var scaleSupport = view as IPlugViewContentScaleSupport;
            window.DpiChanged += (s, e) =>
            {
                scaleSupport?.setContentScaleFactor(e.DeviceDpiNew / 96f);
            };

            try
            {
                var plugViewSize = view.getSize();
                window.ClientSize = new Size(plugViewSize.Width, plugViewSize.Height);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to get initial view size");
            }

            window.HandleCreated += (s, e) =>
            {
                var plugFrame = new PlugFrame((r) => window.ClientSize = new Size(r.Width, r.Height));
                view.setFrame(plugFrame);
                view.attached(window.Handle, "HWND");
                scaleSupport?.setContentScaleFactor(window.DeviceDpi / 96f);
            };
            window.HandleDestroyed += (s, e) =>
            {
                view.removed();
                view.ReleaseComObject();
            };
            window.ClientSizeChanged += (s, e) =>
            {
                var r = window.ClientRectangle;
                view.onSize(new ViewRect() { left = r.Left, right = r.Right, top = r.Top, bottom = r.Bottom });
            };
        }

        window.Visible = true;
    }

    private void HideEditor()
    {
        window?.Close();
        window?.Dispose();
        window = null;
    }

    void IComponentHandler.beginEdit(uint id)
    {
        //throw new NotImplementedException();
    }

    void IComponentHandler.endEdit(uint id)
    {

    }

    void IComponentHandler.performEdit(uint id, double valueNormalized)
    {
        if (!channels.TryGetValue(id, out var x))
            return;

        var (parameter, channel) = x;
        channel.Object = parameter.GetValueAsObject(valueNormalized);
    }

    void IComponentHandler.restartComponent(RestartFlags flags)
    {
        logger.LogTrace("Restarting component with flags {flags}", flags);
    }

    private void SaveToPin<T>(string pinName, T value)
    {
        var solution = IDevSession.Current?.CurrentSolution
            .SetPinValue(nodeContext.Path.Stack, pinName, value);
        solution?.Confirm();
    }

    unsafe void Process(AudioBufferStereo audioBufferStereo)
    {
        if (isDisposed)
            return;

        // Block a potential Dispose call
        processingEvent.Reset();

        inputEvents.Clear();
        var inputEventCount = inputEventQueue.Count;
        while (inputEventCount-- > 0)
        {
            var e = inputEventQueue.Take();
            inputEvents.AddEvent(in e);
        }


        var inputParameterChanges = Interlocked.Exchange(ref this.committedChanges, null);
        var outputParameterChanges = pendingOutputChanges ??= ParameterChangesPool.Default.Get();

        audioBufferStereo.GetConstants(out var numSamples, out var sampleRate, out var startTime);

        var leftOutput = audioBufferStereo.Left.AsSpan();
        var rightOutput = audioBufferStereo.Right.AsSpan();

        // Input and output buffers must be different
        Array.Resize(ref leftInput, leftOutput.Length);
        Array.Resize(ref rightInput, rightOutput.Length);

        leftOutput.CopyTo(leftInput);
        rightOutput.CopyTo(rightInput);

        PrepareBuffers();

        fixed (float* leftInPtr = leftInput)
        fixed (float* rightInPtr = rightInput)
        fixed (float* leftOutPtr = leftOutput)
        fixed (float* rightOutPtr = rightOutput)
        //fixed (float* emptyBufferPtr = emptyBuffer)
        fixed (AudioBusBuffers* audioInputBuffersPtr = audioInputBuffers)
        fixed (AudioBusBuffers* audioOutputBuffersPtr = audioOutputBuffers)
        fixed (ProcessContext* ctx = &processContext)
        {
            var inputBuffers = stackalloc void*[] { leftInPtr, rightInPtr };
            var outputBuffers = stackalloc void*[] { leftOutPtr, rightOutPtr };

            for (int i = 0; i < audioInputBuffers.Length; i++)
                audioInputBuffers[i].channelBuffers = inputBuffers;
            for (int i = 0; i < audioOutputBuffers.Length; i++)
                audioOutputBuffers[i].channelBuffers = outputBuffers;

            var processData = new ProcessData()
            {
                ProcessMode = ProcessModes.Realtime,
                SymbolicSampleSize = processSetup.SymbolicSampleSize,
                // In the unlikely event of the audio buffer getting larger than 4kb
                NumSamples = Math.Min(numSamples, processSetup.MaxSamplesPerBlock),
                NumInputs = audioInputBuffers.Length,
                NumOutputs = audioOutputBuffers.Length,
                Inputs = audioInputBuffersPtr,
                Outputs = audioOutputBuffersPtr,
                InputParameterChanges = (inputParameterChanges ?? s_noChanges).GetComPtr(in IParameterChanges.Guid),
                OutputParameterChanges = outputParameterChanges.GetComPtr(in IParameterChanges.Guid),
                InputEvents = inputEvents.GetComPtr(in IEventList.Guid),
                //OutputEvents = outputEvents.GetComPtr(in IEventList.Guid),
                //ProcessContext = (nint)ctx
            };

            processor.process(in processData);
        }

        // Copy input to output if bypass is enabled and plugin doesn't handle it
        if (byPass && byPassParameter is null)
        {
            leftInput.CopyTo(leftOutput);
            rightInput.CopyTo(rightOutput);
        }

        if (inputParameterChanges != null)
            ParameterChangesPool.Default.Return(inputParameterChanges);

        if (Interlocked.CompareExchange(ref acknowledgedOutputChanges, outputParameterChanges, null) is null)
            pendingOutputChanges = null;

        // Unblock Dispose call
        processingEvent.Set();
    }

    [MemberNotNull(nameof(audioInputBuffers), nameof(audioOutputBuffers))]
    void PrepareBuffers()
    {
        //if (emptyBuffer is null || numSamples > emptyBuffer.Length)
        //{
        //    emptyBuffer = new float[numSamples];
        //    audioInputBuffers = null;
        //    audioOutputBuffers = null;
        //}
        if (audioInputBuffers is null)
            audioInputBuffers = audioInputBusses.Select(b => new AudioBusBuffers() { numChannels = b.ChannelCount }).ToArray();
        if (audioOutputBuffers is null)
            audioOutputBuffers = audioOutputBusses.Select(b => new AudioBusBuffers() { numChannels = b.ChannelCount }).ToArray();
    }
    AudioBusBuffers[]? audioInputBuffers, audioOutputBuffers;
    //float[]? emptyBuffer;

    sealed class AudioIn : IVLPin<IEnumerable<AudioSignal>>
    {
        private readonly BufferCallerSignal bufferCallerSignal;

        public AudioIn(BufferCallerSignal bufferCallerSignal)
        {
            this.bufferCallerSignal = bufferCallerSignal;
        }

        public IEnumerable<AudioSignal>? Value { get => throw new NotImplementedException(); set => bufferCallerSignal.SetInput(value); }
        object? IVLPin.Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    sealed class AudioOut : IVLPin<IReadOnlyList<AudioSignal>>
    {
        private readonly BufferCallerSignal bufferCallerSignal;

        public AudioOut(BufferCallerSignal bufferCallerSignal)
        {
            this.bufferCallerSignal = bufferCallerSignal;
        }

        public IReadOnlyList<AudioSignal>? Value { get => bufferCallerSignal.Outputs; set => throw new NotImplementedException(); }
        object? IVLPin.Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
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
