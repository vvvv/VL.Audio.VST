using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Windows.Forms;
using VL.Core.Import;
using VST3;
using VST3.Hosting;
using IComponent = VST3.IComponent;

namespace VL.Audio.VST;

// Test class to see if we can host a VST3 plugin
[ProcessNode]
internal unsafe partial class AGainFilter : IDisposable
{
    private readonly ManualResetEventSlim processingEvent = new(initialState: true);
    private bool isDisposed;

    private readonly HostApp hostApp = new([]);
    private readonly BufferCallerSignal outputSignal;
    private readonly IAudioProcessor processor;

    private readonly ProcessContext processContext;
    private readonly ParameterChanges inputParameterChanges;
    private readonly ParameterChanges outputParameterChanges;
    private readonly Handler handler;
    private readonly IComponent component;
    private readonly IEditController controller;
    private readonly Form window = new Form();
    private ProcessSetup processSetup;

    public AGainFilter()
    {
        Module.TryCreate("C:\\Users\\elias\\source\\repos\\vst3sdk\\build\\VST3\\Debug\\again.vst3", out var module);
        var factory = module.Factory;

        ComWrappers cw = VstWrappers.Instance;

        var info = module.Factory.ClassInfos[0];
        component = factory.CreateInstance<IComponent>(info.ID);
        component.initialize(hostApp);

        // UI
        Guid cid = default;
        component.getControllerClassId(ref cid);
        controller = factory.CreateInstance<IEditController>(cid);
        controller.initialize(hostApp);

        inputParameterChanges = new ParameterChanges();
        outputParameterChanges = new ParameterChanges();
        handler = new Handler(inputParameterChanges);
        controller.setComponentHandler(handler);

        var view = controller.createView("editor");
        var plugViewSize = view.getSize();
        window.ClientSize = new Size(plugViewSize.Width, plugViewSize.Height);
        window.HandleCreated += (s, e) =>
        {
            var plugFrame = new PlugFrame((r) => { });
            view.setFrame(plugFrame);
            view.attached(window.Handle, "HWND");
        };
        window.HandleDestroyed += (s, e) =>
        {
            view.removed();
            view.ReleaseComObject();
        };
        window.Show();

        processor = (IAudioProcessor)component;

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
                MaxSamplesPerBlock = AudioService.Engine.Settings.BufferSize,
                SampleRate = AudioService.Engine.Settings.SampleRate
            });

        component.activateBus(MediaTypes.kAudio, BusDirections.kInput, 0, true);
        component.activateBus(MediaTypes.kAudio, BusDirections.kOutput, 0, true);;

        component.setActive(true);
        processor.SetProcessing_IgnoreNotImplementedException(true);
    }

    public void Dispose()
    {
        if (isDisposed)
            return;

        isDisposed = true;
        processingEvent.Wait();

        window.Dispose();
        outputSignal.Dispose();
        processor.SetProcessing_IgnoreNotImplementedException(false);
        component.setActive(false);
        component.terminate();
        component.ReleaseComObject();
    }

    [return: Pin(Name = "Output")]
    public IReadOnlyList<AudioSignal> Update(IEnumerable<AudioSignal> input)
    {
        outputSignal.SetInput(input);

        // Notify UI about latest changes
        // TODO: Should be done event based...
        foreach (var (id, v) in outputParameterChanges.GetLatestValues())
            controller.setParamNormalized(id, v);

        return outputSignal.Outputs;
    }


    unsafe void Process(AudioBufferStereo audioBufferStereo)
    {
        if (isDisposed)
            return;

        // Block a potential Dispose call
        processingEvent.Reset();

        audioBufferStereo.GetConstants(out var numSamples, out var sampleRate, out var startTime);

        fixed (float* left = audioBufferStereo.Left)
        fixed (float* right = audioBufferStereo.Right)
        {
            var buffers = stackalloc void*[2];
            buffers[0] = left;
            buffers[1] = right;

            var inputs = new AudioBusBuffers()
            {
                numChannels = 2,
                channelBuffers = buffers
            };
            var outputs = new AudioBusBuffers()
            {
                numChannels = 2,
                channelBuffers = buffers
            };

            var processData = new ProcessData()
            {
                ProcessMode = ProcessModes.Realtime,
                SymbolicSampleSize = processSetup.SymbolicSampleSize,
                NumSamples = numSamples,
                NumInputs = 1,
                NumOutputs = 1,
                Inputs = &inputs,
                Outputs = &outputs,
                InputParameterChanges = inputParameterChanges.ComInterfacePtr,
                OutputParameterChanges = outputParameterChanges.ComInterfacePtr
            };

            processor.process(in processData);
        }

        // Unblock Dispose call
        processingEvent.Set();
    }
}
