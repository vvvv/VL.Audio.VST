namespace VST3;

/// <summary>
/// Audio processing context.
/// For each processing block, the host provides timing information and musical parameters that can
/// change over time. For a host that supports jumps (like cycle), it is possible to split up a
/// processing block into multiple parts in order to provide a correct project time inside of every
/// block, but this behavior is not mandatory. Since the timing will be correct at the beginning of the
/// next block again, a host that is dependent on a fixed processing block size can choose to neglect
/// this problem.
/// </summary>
/// <seealso cref="IAudioProcessor"/>
/// <seealso cref="ProcessData"/>
struct ProcessContext
{
    /// <summary>
    /// Transport state and other flags.
    /// </summary>
    public enum StatesAndFlags : uint
    {
        /// <summary>
        /// Currently playing.
        /// </summary>
        kPlaying = 1 << 1,
        /// <summary>
        /// Cycle is active.
        /// </summary>
        kCycleActive = 1 << 2,
        /// <summary>
        /// Currently recording.
        /// </summary>
        kRecording = 1 << 3,
        /// <summary>
        /// System time contains valid information.
        /// </summary>
        kSystemTimeValid = 1 << 8,
        /// <summary>
        /// Continuous time samples contain valid information.
        /// </summary>
        kContTimeValid = 1 << 17,
        /// <summary>
        /// Project time music contains valid information.
        /// </summary>
        kProjectTimeMusicValid = 1 << 9,
        /// <summary>
        /// Bar position music contains valid information.
        /// </summary>
        kBarPositionValid = 1 << 11,
        /// <summary>
        /// Cycle start music and bar position music contain valid information.
        /// </summary>
        kCycleValid = 1 << 12,
        /// <summary>
        /// Tempo contains valid information.
        /// </summary>
        kTempoValid = 1 << 10,
        /// <summary>
        /// Time signature numerator and time signature denominator contain valid information.
        /// </summary>
        kTimeSigValid = 1 << 13,
        /// <summary>
        /// Chord contains valid information.
        /// </summary>
        kChordValid = 1 << 18,
        /// <summary>
        /// SMPTE offset and frame rate contain valid information.
        /// </summary>
        kSmpteValid = 1 << 14,
        /// <summary>
        /// Samples to next clock valid.
        /// </summary>
        kClockValid = 1 << 15
    };

    /// <summary>
    /// A combination of the values from <see cref="StatesAndFlags"/>.
    /// </summary>
    public StatesAndFlags state;

    /// <summary>
    /// Current sample rate (always valid).
    /// </summary>
    public double sampleRate;
    /// <summary>
    /// Project time in samples (always valid).
    /// </summary>
    public long projectTimeSamples;

    /// <summary>
    /// System time in nanoseconds (optional).
    /// </summary>
    public long systemTime;
    /// <summary>
    /// Project time, without loop (optional).
    /// </summary>
    public long continousTimeSamples;

    /// <summary>
    /// Musical position in quarter notes (1.0 equals 1 quarter note) (optional).
    /// </summary>
    public double projectTimeMusic;
    /// <summary>
    /// Last bar start position, in quarter notes (optional).
    /// </summary>
    public double barPositionMusic;
    /// <summary>
    /// Cycle start in quarter notes (optional).
    /// </summary>
    public double cycleStartMusic;
    /// <summary>
    /// Cycle end in quarter notes (optional).
    /// </summary>
    public double cycleEndMusic;

    /// <summary>
    /// Tempo in BPM (Beats Per Minute) (optional).
    /// </summary>
    public double tempo;
    /// <summary>
    /// Time signature numerator (e.g. 3 for 3/4) (optional).
    /// </summary>
    public int timeSigNumerator;
    /// <summary>
    /// Time signature denominator (e.g. 4 for 3/4) (optional).
    /// </summary>
    public int timeSigDenominator;

    /// <summary>
    /// Musical info (optional).
    /// </summary>
    public Chord chord;

    /// <summary>
    /// SMPTE (sync) offset in subframes (1/80 of frame) (optional).
    /// </summary>
    public int smpteOffsetSubframes;
    /// <summary>
    /// Frame rate (optional).
    /// </summary>
    public FrameRate frameRate;

    /// <summary>
    /// MIDI Clock Resolution (24 Per Quarter Note), can be negative (nearest) (optional).
    /// </summary>
    public int samplesToNextClock;
}
