using System.Buffers;
using System.Runtime.InteropServices;
using VST3;

namespace VL.Audio.VST;

partial class EffectHost
{
    private AudioSignal[] inputAudioSignals = Array.Empty<AudioSignal>();

    private unsafe void FillBuffers(float[][] buffers, int offset, int numSamples)
    {
        lock (processingLock)
        {
            if (isDisposed)
                return;

            // Block a potential Dispose call
            DoFillBuffers(buffers, offset, numSamples);
        }
    }

    private unsafe void DoFillBuffers(float[][] buffers, int offset, int numSamples)
    {
        inputEventList.Clear();
        var inputEventCount = inputEventQueue.Count;
        while (inputEventCount-- > 0)
        {
            var e = inputEventQueue.Take();
            inputEventList.AddEvent(in e);
        }


        var inputParameterChanges = Interlocked.Exchange(ref this.committedChanges, null);
        var outputParameterChanges = pendingOutputChanges ??= ParameterChangesPool.Default.Get();

        // Read inputs
        var arrayPool = ArrayPool<float>.Shared;
        var readBuffer = arrayPool.Rent(numSamples);
        try
        {
            foreach (var inputSignal in inputAudioSignals)
                inputSignal.Read(readBuffer, 0, numSamples);
        }
        finally
        {
            arrayPool.Return(readBuffer);
        }

        // Prepare input channels
        var mainInputChannelCount = audioInputBusses.Length > 0 ? audioInputBusses[0].ChannelCount : 0;
        void** mainInputChannelBuffers = stackalloc void*[mainInputChannelCount];
        for (int i = 0; i < mainInputChannelCount; i++)
        {
            if (i < inputAudioSignals.Length)
                mainInputChannelBuffers[i] = Marshal.UnsafeAddrOfPinnedArrayElement(inputAudioSignals[i].ReadBuffer, 0).ToPointer();
            else
                mainInputChannelBuffers[i] = GetDummySampleBuffer(numSamples).ToPointer();
        }

        // Prepare output channels
        void** mainOutputChannelBuffers = stackalloc void*[buffers.Length];
        for (int i = 0; i < buffers.Length; i++)
            mainOutputChannelBuffers[i] = Marshal.UnsafeAddrOfPinnedArrayElement(buffers[i], offset).ToPointer();

        // Prepare input busses
        AudioBusBuffers* audioInputBuffers = stackalloc AudioBusBuffers[audioInputBusses.Length];
        for (int i = 0; i < audioInputBusses.Length; i++)
        {
            var numChannels = audioInputBuffers[i].numChannels = audioInputBusses[i].ChannelCount;
            if (i == 0)
                audioInputBuffers[i].channelBuffers = mainInputChannelBuffers;
            else
                audioInputBuffers[i].channelBuffers = GetEmptyChannelBuffers(numChannels, processSetup.MaxSamplesPerBlock);
        }

        // Prepare output busses
        AudioBusBuffers* audioOutputBuffers = stackalloc AudioBusBuffers[audioOutputBusses.Length];
        for (int i = 0; i < audioOutputBusses.Length; i++)
        {
            var numChannels = audioOutputBuffers[i].numChannels = audioOutputBusses[i].ChannelCount;
            if (i == 0)
                audioOutputBuffers[i].channelBuffers = mainOutputChannelBuffers;
            else
                audioOutputBuffers[i].channelBuffers = GetEmptyChannelBuffers(numChannels, processSetup.MaxSamplesPerBlock);
        }

        var engine = AudioService.Engine;
        var timer = engine.Timer;

        var processContext = new ProcessContext()
        {
            sampleRate = timer.SampleRate,
            projectTimeSamples = timer.BufferStart,
            systemTime = DateTime.Now.Ticks * 100,
            continousTimeSamples = timer.ContinousSamplePosition,
            projectTimeMusic = timer.Beat,
            barPositionMusic = Math.Floor(timer.Beat / 4) * 4,
            cycleStartMusic = timer.LoopStartBeat,
            cycleEndMusic = timer.LoopEndBeat,
            tempo = timer.BPM,
            timeSigNumerator = timer.TimeSignatureNumerator,
            timeSigDenominator = timer.TimeSignatureDenominator,
            state = (engine.Play ? ProcessContext.StatesAndFlags.kPlaying : default) |
                    ProcessContext.StatesAndFlags.kTempoValid |
                    ProcessContext.StatesAndFlags.kTimeSigValid |
                    ProcessContext.StatesAndFlags.kSystemTimeValid |
                    ProcessContext.StatesAndFlags.kContTimeValid |
                    ProcessContext.StatesAndFlags.kProjectTimeMusicValid |
                    ProcessContext.StatesAndFlags.kCycleValid |
                    ProcessContext.StatesAndFlags.kBarPositionValid |
                    (timer.Loop ? ProcessContext.StatesAndFlags.kCycleActive : default)
        };

        var processData = new ProcessData()
        {
            ProcessMode = ProcessModes.Realtime,
            SymbolicSampleSize = processSetup.SymbolicSampleSize,
            // In the unlikely event of the audio buffer getting larger than 4kb
            NumSamples = Math.Min(numSamples, processSetup.MaxSamplesPerBlock),
            NumInputs = audioInputBusses.Length,
            NumOutputs = audioOutputBusses.Length,
            Inputs = audioInputBuffers,
            Outputs = audioOutputBuffers,
            InputParameterChanges = (inputParameterChanges ?? s_noChanges),
            OutputParameterChanges = outputParameterChanges,
            InputEvents = inputEventList,
            OutputEvents = outputEventList,
            ProcessContext = new nint(&processContext)
        };

        processor.process(in processData);

        // Copy input to output if bypass is enabled and plugin doesn't handle it
        if (!apply && byPassParameter is null)
        {
            // TODO
        }

        if (inputParameterChanges != null)
            ParameterChangesPool.Default.Return(inputParameterChanges);

        if (Interlocked.CompareExchange(ref acknowledgedOutputChanges, outputParameterChanges, null) is null)
            pendingOutputChanges = null;

        // Translate output events to MIDI (if possible) and send them out on background thread
        try
        {
            foreach (var e in outputEventList)
                outputEvents.OnNext(e);
        }
        finally
        {
            outputEventList.Clear();
        }
    }

    private static unsafe void** GetEmptyChannelBuffers(int numChannels, int numSamples)
    {
        if (!s_emptyChannelBuffers.TryGetValue(numChannels, out var buffer))
        {
            s_emptyChannelBuffers[numChannels] = buffer = GC.AllocateArray<IntPtr>(numChannels, pinned: true);
            for (int i = 0; i < numChannels; i++)
                buffer[i] = GetDummySampleBuffer(numSamples);
        }
        return (void**)Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
    }
    private static readonly Dictionary<int, IntPtr[]> s_emptyChannelBuffers = new();

    private static unsafe IntPtr GetDummySampleBuffer(int numSamples)
    {
        if (!s_emptySamplesBuffers.TryGetValue(numSamples, out var buffer))
            s_emptySamplesBuffers[numSamples] = buffer = GC.AllocateArray<float>(numSamples, pinned: true);
        return Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
    }
    private static readonly Dictionary<int, float[]> s_emptySamplesBuffers = new();

    sealed class AudioOutput : MultiChannelSignal
    {
        private readonly EffectHost effectHost;

        public AudioOutput(EffectHost effectHost)
        {
            this.effectHost = effectHost;
        }

        protected override void FillBuffers(float[][] buffers, int offset, int count)
        {
            if (!effectHost.isDisposed)
                effectHost.FillBuffers(buffers, offset, count);
            else
                base.FillBuffers(buffers, offset, count);
        }
    }
}
