using FBCore.Common.Internal;
using System.IO;

namespace FBCore.Common.Util
{
    /// <summary>
    /// Utility method for dealing with Streams.
    /// </summary>
    public class StreamUtil
    {
        /// <summary>
        /// Efficiently fetch bytes from Stream.
        /// </summary>
        public static byte[] GetBytesFromStream(Stream inputStream)
        {
            using (var memoryStream = new MemoryStream())
            {
                inputStream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Skips exactly bytesCount bytes in inputStream unless end of stream is reached first.
        /// </summary>
        /// <param name="inputStream">Input stream to skip bytes from.</param>
        /// <param name="bytesCount">Number of bytes to skip.</param>
        /// <returns>Number of skipped bytes.</returns>
        public static long Skip(Stream inputStream, long bytesCount)
        {
            Preconditions.CheckNotNull(inputStream);
            Preconditions.CheckArgument(bytesCount >= 0);

            long toSkip = bytesCount;
            while (toSkip > 0)
            {
                long current = inputStream.Position;
                long skipped = inputStream.Seek(toSkip, SeekOrigin.Current) - current;
                if (skipped > 0)
                {
                    toSkip -= skipped;
                    continue;
                }

                if (inputStream.ReadByte() != -1)
                {
                    toSkip--;
                    continue;
                }

                return bytesCount - toSkip;
            }

            return bytesCount;
        }
    }
}
