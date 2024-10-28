namespace VST3;

//------------------------------------------------------------------------
/** Audio processing context.
For each processing block the host provides timing information and musical parameters that can
change over time. For a host that supports jumps (like cycle) it is possible to split up a
processing block into multiple parts in order to provide a correct project time inside of every
block, but this behavior is not mandatory. Since the timing will be correct at the beginning of the
next block again, a host that is dependent on a fixed processing block size can choose to neglect
this problem.
\see IAudioProcessor, ProcessData
*/
struct ProcessContext
{
    /** Transport state and other flags */
    public enum StatesAndFlags : uint
    {
        /// <summary>
        /// currently playing
        /// </summary>
        kPlaying = 1 << 1,
        /// <summary>
        /// cycle is active
        /// </summary>
		kCycleActive = 1 << 2,
        /// <summary>
        /// currently recording
        /// </summary>
		kRecording = 1 << 3,
        /// <summary>
        /// systemTime contains valid information
        /// </summary>
        kSystemTimeValid = 1 << 8,
        /// <summary>
        /// continousTimeSamples contains valid information
        /// </summary>
		kContTimeValid = 1 << 17,
        /// <summary>
        /// projectTimeMusic contains valid information
        /// </summary>
        kProjectTimeMusicValid = 1 << 9,
        /// <summary>
        /// barPositionMusic contains valid information
        /// </summary>
		kBarPositionValid = 1 << 11,
        /// <summary>
        /// cycleStartMusic and barPositionMusic contain valid information
        /// </summary>
		kCycleValid = 1 << 12,
        /// <summary>
        /// tempo contains valid information
        /// </summary>
        kTempoValid = 1 << 10,
        /// <summary>
        /// timeSigNumerator and timeSigDenominator contain valid information
        /// </summary>
		kTimeSigValid = 1 << 13,
        /// <summary>
        /// chord contains valid information
        /// </summary>
		kChordValid = 1 << 18,
        /// <summary>
        /// smpteOffset and frameRate contain valid information
        /// </summary>
        kSmpteValid = 1 << 14,
        /// <summary>
        /// samplesToNextClock valid
        /// </summary>
		kClockValid = 1 << 15
	};

    public StatesAndFlags state;

    public double sampleRate;
    public long projectTimeSamples;

    public long systemTime;
    public long continousTimeSamples;

    public double projectTimeMusic;
    public double barPositionMusic;
    public double cycleStartMusic;
    public double cycleEndMusic;

    public double tempo;
    public int timeSigNumerator;
    public int timeSigDenominator;

    public Chord chord;

    public int smpteOffsetSubframes;
    public FrameRate frameRate;

    public int samplesToNextClock;
};
