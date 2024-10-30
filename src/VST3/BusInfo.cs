namespace VST3;

/// <summary>
/// BusInfo:
/// This is the structure used with getBusInfo, informing the host about what is a specific given bus.
/// See also: <see cref="IComponent.getBusInfo"/>
/// </summary>
struct BusInfo
{
    private readonly MediaTypes mediaType;
    private readonly BusDirections direction;
    private readonly int channelCount;
    private readonly String128 name;
    private readonly BusTypes busType;
    private readonly BusFlags flags;

    /// <summary>
    /// Media type - has to be a value of <see cref="MediaTypes"/>.
    /// </summary>
    public MediaTypes MediaType => mediaType;

    /// <summary>
    /// Input or output <see cref="BusDirections"/>.
    /// </summary>
    public BusDirections Direction => direction;

    /// <summary>
    /// Number of channels (if used then need to be rechecked after <see cref="IAudioProcessor.setBusArrangements"/> is called).
    /// For a bus of type <see cref="MediaTypes.kEvent"/>, the channelCount corresponds to the number of supported MIDI channels by this bus.
    /// </summary>
    public int ChannelCount => channelCount;

    /// <summary>
    /// Name of the bus.
    /// </summary>
    public string Name => name.ToString();

    /// <summary>
    /// Main or aux - has to be a value of <see cref="BusTypes"/>.
    /// </summary>
    public BusTypes BusType => busType;

    /// <summary>
    /// Flags - a combination of <see cref="BusFlags"/>.
    /// </summary>
    public BusFlags Flags => flags;

    [Flags]
    public enum BusFlags
    {
        /// <summary>
        /// The bus should be activated by the host per default on instantiation (activateBus call is requested).
        /// By default, a bus is inactive.
        /// </summary>
        DefaultActive = 1 << 0,

        /// <summary>
        /// The bus does not contain ordinary audio data, but data used for control changes at sample rate.
        /// The data is in the same format as the audio data [-1..1].
        /// A host has to prevent unintended routing to speakers to prevent damage.
        /// Only valid for audio media type busses.
        /// [released: 3.7.0]
        /// </summary>
        IsControlVoltage = 1 << 1
    }
}
