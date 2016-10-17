using FBCore.Common.Internal;
using System.IO;

namespace Cache.Common
{
    /// <summary>
    /// Utility class to create typical <see cref="IWriterCallback"/>.
    /// </summary>
    public class WriterCallbacks
    {
        /// <summary>
        /// Creates a writer callback that copies all the content read from an <see cref="Stream"/> into
        /// the target stream.
        ///
        /// <para />This writer can be used only once.
        /// <param name="inputStream">the source</param>
        /// @return the writer callback
        /// </summary>
        public static IWriterCallback From(Stream inputStream)
        {
            return new WriterCallbackStream(inputStream);
        }

        /// <summary>
        /// Creates a writer callback that writes some byte array to the target stream.
        ///
        /// <para />This writer can be used many times.
        /// <param name="data">the bytes to write</param>
        /// @return the writer callback
        /// </summary>
        public static IWriterCallback From(byte[] data)
        {
            return new WriterCallbackByteArray(data);
        }

        class WriterCallbackStream : IWriterCallback
        {
            private Stream _inputStream;

            public WriterCallbackStream(Stream inputStream)
            {
                _inputStream = inputStream;
            }

            public void Write(Stream outputStream)
            {
                ByteStreams.Copy(_inputStream, outputStream);
            }
        }

        class WriterCallbackByteArray : IWriterCallback
        {
            private byte[] _data;

            public WriterCallbackByteArray(byte[] data)
            {
                _data = data;
            }

            public void Write(Stream outputStream)
            {
                outputStream.Write(_data, 0, _data.Length);
            }
        }
    }
}
