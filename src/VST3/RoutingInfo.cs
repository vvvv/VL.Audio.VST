namespace VST3;

//------------------------------------------------------------------------
/** Routing Information:
When the plug-in supports multiple I/O busses, a host may want to know how the busses are related. The
relation of an event-input-channel to an audio-output-bus in particular is of interest to the host
(in order to relate MIDI-tracks to audio-channels)
\n See also: IComponent::getRoutingInfo, \ref vst3Routing
*/
struct RoutingInfo
{
    /// <summary>
    /// media type see \ref MediaTypes
    /// </summary>
    MediaTypes mediaType;
    /// <summary>
    /// bus index
    /// </summary>
	int busIndex;
    /// <summary>
    /// channel (-1 for all channels)
    /// </summary>
	int channel;
};
