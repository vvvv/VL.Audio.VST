namespace VST3;

//------------------------------------------------------------------------
/** BusInfo:
This is the structure used with getBusInfo, informing the host about what is a specific given bus.
\n See also: Steinberg::Vst::IComponent::getBusInfo
*/
struct BusInfo
{
    ///<summary>
    /// Media type - has to be a value of <see cref="MediaTypes"/>.
    ///</summary>
    MediaTypes mediaType;

    ///<summary>
    /// Input or output <see cref="BusDirections"/>.
    ///</summary>
    BusDirections direction;

    ///<summary>
    /// Number of channels (if used then need to be rechecked after <see cref="IAudioProcessor.setBusArrangements"/> is called).
    /// For a bus of type <see cref="MediaTypes.kEvent"/>, the channelCount corresponds to the number of supported MIDI channels by this bus.
    ///</summary>
    int channelCount;

    ///<summary>
    /// Name of the bus.
    ///</summary>
    String128 name;

    ///<summary>
    /// Main or aux - has to be a value of <see cref="BusTypes"/>.
    ///</summary>
    BusTypes busType;

    ///<summary>
    /// Flags - a combination of <see cref="BusFlags"/>.
    ///</summary>
    BusFlags flags;

    [Flags]
    enum BusFlags
    {
        ///<summary>
        /// The bus should be activated by the host per default on instantiation (activateBus call is requested).
        /// By default, a bus is inactive.
        ///</summary>
        kDefaultActive = 1 << 0,

        ///<summary>
        /// The bus does not contain ordinary audio data, but data used for control changes at sample rate.
        /// The data is in the same format as the audio data [-1..1].
        /// A host has to prevent unintended routing to speakers to prevent damage.
        /// Only valid for audio media type busses.
        /// [released: 3.7.0]
        ///</summary>
        kIsControlVoltage = 1 << 1
    };
};
