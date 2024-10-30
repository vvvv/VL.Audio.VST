namespace VST3;

/// <summary>
/// Any data needed in audio processing.
/// The host prepares AudioBusBuffers for each input/output bus,
/// regardless of the bus activation state. Bus buffer indices always match
/// with bus indices used in IComponent::getBusInfo of media type kAudio.
/// </summary>
/// <remarks>
/// See <see cref="AudioBusBuffers"/>, <see cref="IParameterChanges"/>, <see cref="IEventList"/>, <see cref="ProcessContext"/>, <see cref="IProcessContextRequirements"/>.
/// </remarks>
unsafe struct ProcessData
{
    /// <summary>
    /// Processing mode - value of <see cref="ProcessModes"/>.
    /// </summary>
    public ProcessModes ProcessMode;

    /// <summary>
    /// Sample size - value of <see cref="SymbolicSampleSizes"/>.
    /// </summary>
    public SymbolicSampleSizes SymbolicSampleSize;

    /// <summary>
    /// Number of samples to process.
    /// </summary>
    public int NumSamples;

    /// <summary>
    /// Number of input busses.
    /// </summary>
    public int NumInputs;

    /// <summary>
    /// Number of output busses.
    /// </summary>
    public int NumOutputs;

    /// <summary>
    /// Buffers of input busses.
    /// </summary>
    public AudioBusBuffers* Inputs;

    /// <summary>
    /// Buffers of output busses.
    /// </summary>
    public AudioBusBuffers* Outputs;

    /// <summary>
    /// Incoming parameter changes for this block.
    /// </summary>
    public nint InputParameterChanges;

    /// <summary>
    /// Outgoing parameter changes for this block (optional).
    /// </summary>
    public nint OutputParameterChanges;

    /// <summary>
    /// Incoming events for this block (optional).
    /// </summary>
    public nint InputEvents;

    /// <summary>
    /// Outgoing events for this block (optional).
    /// </summary>
    public nint OutputEvents;

    /// <summary>
    /// Processing context (optional, but most welcome).
    /// </summary>
    public nint ProcessContext;
}
