using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace VST3;

//------------------------------------------------------------------------
/** Base class for streams.
\ingroup pluginBase
- read/write binary data from/to stream
- get/set stream read-write position (read and write position is the same)
*/
[GeneratedComInterface]
[Guid("C3BF6EA2-3099-4752-9B6B-F9901EE33E9B")]
partial interface IBStream
{
    public enum IStreamSeekMode
    {
        /// <summary>
        /// set absolute seek position
        /// </summary>
        kIBSeekSet = 0,
        /// <summary>
        /// set seek position relative to current position
        /// </summary>
        kIBSeekCur,
        /// <summary>
        /// set seek position relative to stream end
        /// </summary>
        kIBSeekEnd
    }

    //------------------------------------------------------------------------
    /** Reads binary data from stream.
	\param buffer : destination buffer
	\param numBytes : amount of bytes to be read
	\param numBytesRead : result - how many bytes have been read from stream (set to 0 if this is of no interest) */
    unsafe void read(nint buffer, int numBytes, int* numBytesRead = default);

    /** Writes binary data to stream.
	\param buffer : source buffer
	\param numBytes : amount of bytes to write
	\param numBytesWritten : result - how many bytes have been written to stream (set to 0 if this is of no interest) */
    unsafe void write(nint buffer, int numBytes, int* numBytesWritten = default);

    /** Sets stream read-write position. 
	\param pos : new stream position (dependent on mode)
	\param mode : value of enum IStreamSeekMode
	\param result : new seek position (set to 0 if this is of no interest) */
    unsafe void seek(long pos, IStreamSeekMode mode, long* result = default);

    /** Gets current stream read-write position. 
	\param pos : is assigned the current position if function succeeds */
    void tell(out long pos);
};
