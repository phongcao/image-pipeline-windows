using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace ImagePipeline.NativeCode
{
    /// <summary>
    /// <see cref="IStream"/> wrapper class for <see cref="Stream"/>.
    /// </summary>
    internal sealed class ManagedIStream : IStream, IDisposable
    {
        private const int STREAM_SEEK_SET = 0x0;
        private const int STREAM_SEEK_CUR = 0x1;
        private const int STREAM_SEEK_END = 0x2;

        private readonly Stream _internalStream;

        internal ManagedIStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("ioStream");
            }

            _internalStream = stream;
        }

        /// <summary>
        /// Gets the internal stream.
        /// </summary>
        /// <returns>The internal stream.</returns>
        internal Stream GetStream()
        {
            return _internalStream;
        }

        /// <summary>
        /// Cleanup resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_internalStream != null)
                {
                    _internalStream.Dispose();
                }
            }
        }

        /// <summary>
        /// Cleanup resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Reads a specified number of bytes from the stream object into memory starting
        /// at the current seek pointer.
        /// </summary>
        /// <param name="pv">
        /// When this method returns, contains the data read from the stream. This parameter
        /// is passed uninitialized.
        /// </param>
        /// <param name="cb">The number of bytes to read from the stream object.</param>
        /// <param name="pcbRead">
        /// A pointer to a ULONG variable that receives the actual number of bytes read from
        /// the stream object.
        /// </param>
        void IStream.Read(byte[] pv, int cb, IntPtr pcbRead)
        {
            int bytesRead = _internalStream.Read(pv, 0, cb);
            if (pcbRead != IntPtr.Zero)
            {
                Marshal.WriteInt32(pcbRead, bytesRead);
            }
        }

        /// <summary>
        /// Changes the seek pointer to a new location relative to the beginning of the stream,
        /// to the end of the stream, or to the current seek pointer.
        /// </summary>
        /// <param name="dlibMove">The displacement to add to dwOrigin.</param>
        /// <param name="dwOrigin">
        /// The origin of the seek. The origin can be the beginning of the file, the current
        /// seek pointer, or the end of the file.
        /// </param>
        /// <param name="plibNewPosition">
        /// On successful return, contains the offset of the seek pointer from the beginning
        /// of the stream.
        /// </param>
        void IStream.Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
        {
            SeekOrigin origin;
            switch (dwOrigin)
            {
                case STREAM_SEEK_SET:
                    origin = SeekOrigin.Begin;
                    break;

                case STREAM_SEEK_CUR:
                    origin = SeekOrigin.Current;
                    break;

                case STREAM_SEEK_END:
                    origin = SeekOrigin.End;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("dwOrigin");
            }

            long position = _internalStream.Seek(dlibMove, origin);
            if (plibNewPosition != IntPtr.Zero)
            {
                Marshal.WriteInt64(plibNewPosition, position);
            }
        }

        /// <summary>
        /// Changes the size of the stream object.
        /// </summary>
        /// <param name="libNewSize">
        /// The new size of the stream as a number of bytes.
        /// </param>
        void IStream.SetSize(long libNewSize)
        {
            _internalStream.SetLength(libNewSize);
        }

        /// <summary>
        /// Writes a specified number of bytes into the stream object starting at the current
        /// seek pointer.
        /// </summary>
        /// <param name="pv">The buffer to write this stream to.</param>
        /// <param name="cb">The number of bytes to write to the stream.</param>
        /// <param name="pcbWritten">
        /// On successful return, contains the actual number of bytes written to the stream
        /// object. If the caller sets this pointer to System.IntPtr.Zero, this method does
        /// not provide the actual number of bytes written.
        /// </param>
        void IStream.Write(byte[] pv, int cb, IntPtr pcbWritten)
        {
            _internalStream.Write(pv, 0, cb);
            if (pcbWritten != IntPtr.Zero)
            {
                Marshal.WriteInt32(pcbWritten, cb);
            }
        }

        /// <summary>
        /// Retrieves the System.Runtime.InteropServices.STATSTG structure for this stream.
        /// </summary>
        /// <param name="pstatstg">
        /// When this method returns, contains a STATSTG structure that describes this stream
        /// object. This parameter is passed uninitialized.
        /// </param>
        /// <param name="grfStatFlag">
        /// Members in the STATSTG structure that this method does not return, thus saving
        /// some memory allocation operations.
        /// </param>
        void IStream.Stat(out STATSTG pstatstg, int grfStatFlag)
        {
            pstatstg = new STATSTG();
            pstatstg.cbSize = _internalStream.Length;
        }

        void IStream.CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {
            throw new NotSupportedException();
        }

        void IStream.Commit(int grfCommitFlags)
        {
            throw new NotSupportedException();
        }

        void IStream.Revert()
        {
            throw new NotSupportedException();
        }

        void IStream.LockRegion(long libOffset, long cb, int dwLockType)
        {
            throw new NotSupportedException();
        }

        void IStream.UnlockRegion(long libOffset, long cb, int dwLockType)
        {
            throw new NotSupportedException();
        }

        void IStream.Clone(out IStream ppstm)
        {
            throw new NotSupportedException();
        }
    }
}
