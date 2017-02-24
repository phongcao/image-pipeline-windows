using System;
using System.Text;

namespace FBCore.Common.Webp
{
    /// <summary>
    /// WebP helper class.
    /// </summary>
    public class WebpSupportStatus
    {
        /// <summary>
        /// BASE64 encoded extended WebP image.
        /// </summary>
        private const string VP8X_WEBP_BASE64 = "UklGRkoAAABXRUJQVlA4WAoAAAAQAAAAAAAAAAAAQUxQSAw" +
            "AAAARBxAR/Q9ERP8DAABWUDggGAAAABQBAJ0BKgEAAQAAAP4AAA3AAP7mtQAAAA==";

        /// <summary>
        /// Helper method that transforms provided string into its byte representation
        /// using ASCII encoding.
        /// </summary>
        /// <param name="value">Bytes value.</param>
        /// <returns>Byte array representing ascii encoded value.</returns>
        private static byte[] AsciiBytes(string value)
        {
            try
            {
                return Encoding.ASCII.GetBytes(value);
            }
            catch (EncoderFallbackException uee)
            {
                // Won't happen
                throw new Exception("ASCII not found!", uee);
            }
        }


        /// <summary>
        /// Each WebP header should consist of at least 20 bytes and start
        /// with "RIFF" bytes followed by some 4 bytes and "WEBP" bytes.
        /// A more detailed description if WebP can be found here:
        /// <a href="https://developers.google.com/speed/webp/docs/riff_container">
        ///   https://developers.google.com/speed/webp/docs/riff_container</a>
        /// </summary>
        private const int SIMPLE_WEBP_HEADER_LENGTH = 20;

        /// <summary>
        /// Each VP8X WebP image has a "features" byte following its ChunkHeader('VP8X').
        /// </summary>
        private const int EXTENDED_WEBP_HEADER_LENGTH = 21;

        private static readonly byte[] WEBP_RIFF_BYTES = AsciiBytes("RIFF");
        private static readonly byte[] WEBP_NAME_BYTES = AsciiBytes("WEBP");

        /// <summary>
        /// This is a constant used to detect different WebP's formats: vp8, vp8l and vp8x.
        /// </summary>
        private static readonly byte[] WEBP_VP8_BYTES = AsciiBytes("VP8 ");
        private static readonly byte[] WEBP_VP8L_BYTES = AsciiBytes("VP8L");
        private static readonly byte[] WEBP_VP8X_BYTES = AsciiBytes("VP8X");

        /// <summary>
        /// Checks if imageHeaderBytes is AnimatedWebp.
        /// </summary>
        public static bool IsAnimatedWebpHeader(byte[] imageHeaderBytes, int offset)
        {
            bool isVp8x = MatchBytePattern(imageHeaderBytes, offset + 12, WEBP_VP8X_BYTES);

            // ANIM is 2nd bit (00000010 == 2) on 21st byte (imageHeaderBytes[20])
            bool hasAnimationBit = (imageHeaderBytes[offset + 20] & 2) == 2;
            return isVp8x && hasAnimationBit;
        }

        /// <summary>
        /// Checks if imageHeaderBytes is SimpleWebp.
        /// </summary>
        public static bool IsSimpleWebpHeader(byte[] imageHeaderBytes,int offset)
        {
            return MatchBytePattern(imageHeaderBytes, offset + 12, WEBP_VP8_BYTES);
        }

        /// <summary>
        /// Checks if imageHeaderBytes is LosslessWebp.
        /// </summary>
        public static bool IsLosslessWebpHeader(byte[] imageHeaderBytes,int offset)
        {
            return MatchBytePattern(imageHeaderBytes, offset + 12, WEBP_VP8L_BYTES);
        }

        /// <summary>
        /// Checks if imageHeaderBytes is ExtendedWebp.
        /// </summary>
        public static bool IsExtendedWebpHeader(
            byte[] imageHeaderBytes,
            int offset,
            int headerSize)
        {
            return headerSize >= EXTENDED_WEBP_HEADER_LENGTH &&
                MatchBytePattern(imageHeaderBytes, offset + 12, WEBP_VP8X_BYTES);
        }

        /// <summary>
        /// Checks if imageHeaderBytes is ExtendedWebpWithAlpha.
        /// </summary>
        public static bool IsExtendedWebpHeaderWithAlpha(
            byte[] imageHeaderBytes,
            int offset)
        {
            bool isVp8x = MatchBytePattern(imageHeaderBytes, offset + 12, WEBP_VP8X_BYTES);

            // Has ALPHA is 5th bit (00010000 == 16) on 21st byte (imageHeaderBytes[20])
            bool hasAlphaBit = (imageHeaderBytes[offset + 20] & 16) == 16;
            return isVp8x && hasAlphaBit;
        }

        /// <summary>
        /// Checks if imageHeaderBytes contains WEBP_RIFF_BYTES and WEBP_NAME_BYTES and if the
        /// header is long enough to be WebP's header.
        /// WebP file format can be found here:
        /// <a href="https://developers.google.com/speed/webp/docs/riff_container">
        ///   https://developers.google.com/speed/webp/docs/riff_container</a>
        /// </summary>
        /// <returns>true if imageHeaderBytes contains a valid webp header.</returns>
        public static bool IsWebpHeader(
            byte[] imageHeaderBytes,
            int offset,
            int headerSize)
        {
            return headerSize >= SIMPLE_WEBP_HEADER_LENGTH &&
                MatchBytePattern(imageHeaderBytes, offset, WEBP_RIFF_BYTES) &&
                MatchBytePattern(imageHeaderBytes, offset + 8, WEBP_NAME_BYTES);
        }

        private static bool MatchBytePattern(
            byte[] byteArray,
            int offset,
            byte[] pattern)
        {
            if (pattern == null || byteArray == null)
            {
                return false;
            }

            if (pattern.Length + offset > byteArray.Length)
            {
                return false;
            }

            for (int i = 0; i < pattern.Length; ++i)
            {
                if (byteArray[i + offset] != pattern[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
