using System.Runtime.InteropServices.Marshalling;

namespace VST3.Hosting
{
    [GeneratedComClass]
    sealed partial class BStreamAdapter : IBStream
    {
        private readonly Stream stream;

        public BStreamAdapter(Stream stream)
        {
            this.stream = stream;
        }

        public Stream Stream => stream;

        unsafe void IBStream.read(nint buffer, int numBytes, int* numBytesRead)
        {
            var span = new Span<byte>(buffer.ToPointer(), numBytes);
            var r = stream.Read(span);
            if (numBytesRead != null)
                *numBytesRead = r;
        }

        unsafe void IBStream.seek(long pos, IBStream.IStreamSeekMode mode, long* result)
        {
            var r = stream.Seek(pos, (SeekOrigin)mode);
            if (result != null)
                *result = r;
        }

        void IBStream.tell(out long pos)
        {
            pos = stream.Position;
        }

        unsafe void IBStream.write(nint buffer, int numBytes, int* numBytesWritten)
        {
            var span = new ReadOnlySpan<byte>(buffer.ToPointer(), numBytes);
            stream.Write(span);
            if (numBytesWritten != null)
                *numBytesWritten = span.Length;
        }
    }
}
