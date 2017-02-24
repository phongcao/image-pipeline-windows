using ImagePipeline.Memory;
using System;

namespace ImagePipeline.Testing
{
    /// <summary>
    /// A fake implementation of <see cref="NativeMemoryChunk"/> to allow us to
    /// test out pools and other functionality. This uses byte arrays instead of
    /// actual native memory, but supports the same public interface.
    /// </summary>
    public class FakeNativeMemoryChunk : NativeMemoryChunk
    {
        private byte[] _buf;

        /// <summary>
        /// Instantiates the <see cref="FakeNativeMemoryChunk"/>.
        /// </summary>
        /// <param name="bufSize">Size.</param>
        public FakeNativeMemoryChunk(int bufSize)
        {
            _buf = new byte[bufSize];
        }

        /// <summary>
        /// Fake Dispose method.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _buf = null;
        }

        /// <summary>
        /// Fake Closed attribute.
        /// </summary>
        public override bool Closed
        {
            get
            {
                return _buf == null;
            }
        }

        /// <summary>
        /// Fake Size.
        /// </summary>
        public override int Size
        {
            get
            {
                return _buf.Length;
            }
        }

        /// <summary>
        ///  Fake Write method.
        /// </summary>
        public override int Write(
            int nativeMemoryOffset, 
            byte[] byteArray, 
            int byteArrayOffset, 
            int count)
        {
            int numToWrite = Math.Min(count, _buf.Length - nativeMemoryOffset);
            Array.Copy(byteArray, byteArrayOffset, _buf, nativeMemoryOffset, numToWrite);
            return numToWrite;
        }

        /// <summary>
        /// Fake Read method.
        /// </summary>
        public override byte Read(int nativeMemoryOffset)
        {
            return _buf[nativeMemoryOffset];
        }

        /// <summary>
        /// Fake Read method.
        /// </summary>
        public override int Read(
            int nativeMemoryOffset, 
            byte[] byteArray, 
            int byteArrayOffset, 
            int count)
        {
            int numToRead = Math.Min(count, _buf.Length - nativeMemoryOffset);
            Array.Copy(_buf, nativeMemoryOffset, byteArray, byteArrayOffset, numToRead);
            return numToRead;
        }

        /// <summary>
        /// Fake Copy method.
        /// </summary>
        public override void Copy(int offset, NativeMemoryChunk other, int otherOffset, int count)
        {
            FakeNativeMemoryChunk that = (FakeNativeMemoryChunk)other;
            int numToCopy = Math.Min(count, _buf.Length - offset);
            Array.Copy(_buf, offset, that._buf, otherOffset, numToCopy);
        }
    }
}
