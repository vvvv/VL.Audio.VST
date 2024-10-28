namespace VST3;

struct ProcessSetup
{
    public ProcessModes ProcessMode;
    public SymbolicSampleSizes SymbolicSampleSize;
    /// <summary>
    /// maximum number of samples per audio block
    /// </summary>
    public int MaxSamplesPerBlock;
    public double SampleRate;
}
