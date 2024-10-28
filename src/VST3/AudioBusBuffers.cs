namespace VST3;

//------------------------------------------------------------------------
/** Processing buffers of an audio bus.
This structure contains the processing buffer for each channel of an audio bus.
- The number of channels (numChannels) must always match the current bus arrangement.
  It could be set to value '0' when the host wants to flush the parameters (when the plug-in is not processed).
- The size of the channel buffer array must always match the number of channels. So the host
  must always supply an array for the channel buffers, regardless if the
  bus is active or not. However, if an audio bus is currently inactive, the actual sample
  buffer addresses are safe to be null.
- The silence flag is set when every sample of the according buffer has the value '0'. It is
  intended to be used as help for optimizations allowing a plug-in to reduce processing activities.
  But even if this flag is set for a channel, the channel buffers must still point to valid memory!
  This flag is optional. A host is free to support it or not.
.
\see ProcessData
*/
unsafe struct AudioBusBuffers
{
    /// <summary>
    /// number of audio channels in bus
    /// </summary>
    public int numChannels;
    /// <summary>
    /// Bitset of silence state per channel
    /// </summary>
	public ulong silenceFlags;
    /// <summary>
    /// sample buffers to process with 32-bit or 64-bit precision
    /// </summary>
    public void** channelBuffers;
};
