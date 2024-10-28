namespace VST3;

//------------------------------------------------------------------------
/** Any data needed in audio processing.
	The host prepares AudioBusBuffers for each input/output bus,
	regardless of the bus activation state. Bus buffer indices always match
	with bus indices used in IComponent::getBusInfo of media type kAudio.
\see AudioBusBuffers, IParameterChanges, IEventList, ProcessContext, IProcessContextRequirements
*/
unsafe struct ProcessData
{
    public ProcessModes processMode;         // processing mode - value of \ref ProcessModes
    public SymbolicSampleSizes symbolicSampleSize;   // sample size - value of \ref SymbolicSampleSizes
    public int numSamples;          // number of samples to process
    public int numInputs;
    public int numOutputs;
    public AudioBusBuffers* inputs;   // buffers of input busses
    public AudioBusBuffers* outputs;   // buffers of output busses

    public nint inputParameterChanges; // incoming parameter changes for this block
    public nint outputParameterChanges;    // outgoing parameter changes for this block (optional)
    public nint inputEvents;              // incoming events for this block (optional)
    public nint outputEvents;             // outgoing events for this block (optional)
    public nint processContext;			// processing context (optional, but most welcome)
};
