using FBCore.Common.Internal;
using System;
using System.IO;

namespace ImagePipelineBase.ImageFormatUtils
{
    /// <summary>
    /// Detects the format of an encoded gif.
    /// </summary>
    public class GifFormatChecker
    {
        private const int FRAME_HEADER_SIZE = 10;

        /// <summary>
        /// Every GIF frame header starts with a 4 byte static sequence consisting of the following bytes
        /// </summary>
        private static readonly byte[] FRAME_HEADER_START = new byte[] 
        {
            0x00, 0x21, 0xF9, 0x04
        };

        /// <summary>
        /// Every GIF frame header ends with a 2 byte static sequence consisting of one of
        /// the following two sequences of bytes
        /// </summary>
        private static readonly byte[] FRAME_HEADER_END_1 = new byte[]
        {
            0x00, 0x2C
        };

        private static readonly byte[] FRAME_HEADER_END_2 = new byte[]
        {
            0x00, 0x21
        };

        private GifFormatChecker() { }

        /// <summary>
        /// Checks if source contains more than one frame header in it in order to decide whether a GIF
        /// image is animated or not.
        ///
        /// @return true if source contains more than one frame header in its bytes
        /// </summary>
        public static bool IsAnimated(Stream source)
        {
            byte[] buffer = new byte[FRAME_HEADER_SIZE];

            try
            {
                source.Read(buffer, 0, FRAME_HEADER_SIZE);

                int offset = 0;
                int frameHeaders = 0;

                // Read bytes into a circular buffer and check if it matches one of the frame header
                // sequences. First byte can be ignored as it will be part of the GIF static header.
                while (source.Read(buffer, offset, 1) > 0)
                {
                    // This sequence of bytes might be found in the data section of the file, worst case
                    // scenario this method will return true meaning that a static gif is animated.
                    if (CircularBufferMatchesBytePattern(buffer, offset + 1, FRAME_HEADER_START) && 
                        (CircularBufferMatchesBytePattern(buffer, offset + 9, FRAME_HEADER_END_1) || 
                         CircularBufferMatchesBytePattern(buffer, offset + 9, FRAME_HEADER_END_2)))
                    {
                        frameHeaders++;
                        if (frameHeaders > 1)
                        {
                            return true;
                        }
                    }

                    offset = (offset + 1) % buffer.Length;
                }
            }
            catch (IOException ioe)
            {
                throw ioe;
            }

            return false;
        }

        /// <summary>
        /// Checks if the byte array matches a pattern.
        ///
        /// <para />Instead of doing a normal scan, we treat the array as a circular buffer, with 'offset'
        /// determining the start point.
        ///
        /// @return true if match succeeds, false otherwise
        /// </summary>
        internal static bool CircularBufferMatchesBytePattern(
          byte[] byteArray, int offset, byte[] pattern)
        {
            Preconditions.CheckNotNull(byteArray);
            Preconditions.CheckNotNull(pattern);
            Preconditions.CheckArgument(offset >= 0);
            if (pattern.Length > byteArray.Length)
            {
                return false;
            }

            for (int i = 0; i < pattern.Length; i++)
            {
                if (byteArray[(i + offset) % byteArray.Length] != pattern[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
