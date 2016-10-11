using FBCore.Common.Internal;
using FBCore.Common.Webp;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageFormatUtils
{
    /// <summary>
    /// Detects the format of an encoded image.
    /// </summary>
    public class ImageFormatChecker
    {
        /// <summary>
        /// Each WebP header should cosist of at least 20 bytes and start
        /// with "RIFF" bytes followed by some 4 bytes and "WEBP" bytes.
        /// More detailed description if WebP can be found here:
        /// <a href="https://developers.google.com/speed/webp/docs/riff_container">
        ///   https://developers.google.com/speed/webp/docs/riff_container</a>
        /// </summary>
        private const int SIMPLE_WEBP_HEADER_LENGTH = 20;

        /// <summary>
        /// Each VP8X WebP image has "features" byte following its ChunkHeader('VP8X')
        /// </summary>
        private const int EXTENDED_WEBP_HEADER_LENGTH = 21;

        /// <summary>
        /// Every JPEG image should start with SOI mark (0xFF, 0xD8) followed by beginning
        /// of another segment (0xFF)
        /// </summary>
        private static readonly byte[] JPEG_HEADER = new byte[] { 0xFF, 0xD8, 0xFF };

        /// <summary>
        /// Every PNG image starts with 8 byte signature consisting of
        /// following bytes
        /// </summary>
        private static readonly byte[] PNG_HEADER = new byte[] 
        {
            0x89,
            (byte)'P', (byte)'N', (byte)'G',
            0x0D, 0x0A, 0x1A, 0x0A
        };

        /// <summary>
        /// Every gif image starts with "GIF" bytes followed by
        /// bytes indicating version of gif standard
        /// </summary>
        private static readonly byte[] GIF_HEADER_87A = AsciiBytes("GIF87a");
        private static readonly byte[] GIF_HEADER_89A = AsciiBytes("GIF89a");
        private const int GIF_HEADER_LENGTH = 6;

        /// <summary>
        /// Every bmp image starts with "BM" bytes
        /// </summary>
        private static readonly byte[] BMP_HEADER = AsciiBytes("BM");

        /// <summary>
        /// Maximum header size for any image type.
        ///
        /// <para />This determines how much data <see cref="GetImageFormat(Stream)" />
        /// reads from a stream. After changing any of the type detection algorithms, or adding a new one,
        /// this value should be edited.
        /// </summary>
        private static readonly int MAX_HEADER_LENGTH = new []
        {
            EXTENDED_WEBP_HEADER_LENGTH,
            SIMPLE_WEBP_HEADER_LENGTH,
            JPEG_HEADER.Length,
            PNG_HEADER.Length,
            GIF_HEADER_LENGTH,
            BMP_HEADER.Length
        }.Max();

        private ImageFormatChecker() { }

        /// <summary>
        /// Tries to match imageHeaderByte and headerSize against every known image format.
        /// If any match succeeds, corresponding ImageFormat is returned.
        /// <param name="imageHeaderBytes"></param>
        /// <param name="headerSize"></param>
        /// @return ImageFormat for given imageHeaderBytes or UNKNOWN if no such type could be recognized
        /// </summary>
        private static ImageFormat DoGetImageFormat(
            byte[] imageHeaderBytes,
            int headerSize)
        {
            Preconditions.CheckNotNull(imageHeaderBytes);

            if (WebpSupportStatus.IsWebpHeader(imageHeaderBytes, 0, headerSize))
            {
                return GetWebpFormat(imageHeaderBytes, headerSize);
            }

            if (IsJpegHeader(imageHeaderBytes, headerSize))
            {
                return ImageFormat.JPEG;
            }

            if (IsPngHeader(imageHeaderBytes, headerSize))
            {
                return ImageFormat.PNG;
            }

            if (IsGifHeader(imageHeaderBytes, headerSize))
            {
                return ImageFormat.GIF;
            }

            if (IsBmpHeader(imageHeaderBytes, headerSize))
            {
                return ImageFormat.BMP;
            }

            return ImageFormat.UNKNOWN;
        }

        /// <summary>
        /// Reads up to MAX_HEADER_LENGTH bytes from is InputStream. If mark is supported by is, it is
        /// used to restore content of the stream after appropriate amount of data is read.
        /// Read bytes are stored in imageHeaderBytes, which should be capable of storing
        /// MAX_HEADER_LENGTH bytes.
        /// <param name="inputStream"></param>
        /// <param name="imageHeaderBytes"></param>
        /// @return number of bytes read from is
        /// @throws IOException
        /// </summary>
        private static int ReadHeaderFromStream(
            Stream inputStream,
            byte[] imageHeaderBytes)
        {
            Preconditions.CheckNotNull(inputStream);
            Preconditions.CheckNotNull(imageHeaderBytes);
            Preconditions.CheckArgument(imageHeaderBytes.Length >= MAX_HEADER_LENGTH);
            return ByteStreams.Read(inputStream, imageHeaderBytes, 0, MAX_HEADER_LENGTH);
        }

        /// <summary>
        /// Tries to read up to MAX_HEADER_LENGTH bytes from InputStream is and use read bytes to
        /// determine type of the image contained in is. If provided input stream does not support mark,
        /// then this method consumes data from is and it is not safe to read further bytes from is after
        /// this method returns. Otherwise, if mark is supported, it will be used to preserve oryginal
        /// content of inputStream.
        /// <param name="inputStream"></param>
        /// @return ImageFormat matching content of is InputStream or UNKNOWN if no type is suitable
        /// @throws IOException if exception happens during read
        /// </summary>
        public static ImageFormat GetImageFormat(Stream inputStream)
        {
            Preconditions.CheckNotNull(inputStream);
            byte[] imageHeaderBytes = new byte[MAX_HEADER_LENGTH];
            int headerSize = ReadHeaderFromStream(inputStream, imageHeaderBytes);
            return DoGetImageFormat(imageHeaderBytes, headerSize);
        }

        /// <summary>
        /// A variant of getImageFormat that wraps IOException with RuntimeException.
        /// This relieves clients of implementing dummy rethrow try-catch block.
        /// </summary>
        public static ImageFormat GetImageFormat_WrapIOException(Stream inputStream)
        {
            try
            {
                return GetImageFormat(inputStream);
            }
            catch (IOException ioe)
            {
                throw ioe;
            }
        }

        /// <summary>
        /// Reads image header from a file indicated by provided filename and determines
        /// its format. This method does not throw IOException if one occurs. In this case,
        /// ImageFormat.UNKNOWN will be returned.
        /// <param name="filename"></param>
        /// @return ImageFormat for image stored in filename
        /// </summary>
        public static ImageFormat GetImageFormat(string filename)
        {
            FileStream fileInputStream = null;

            try
            {
                fileInputStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                return GetImageFormat(fileInputStream);
            }
            catch (IOException)
            {
                return ImageFormat.UNKNOWN;
            }
            finally
            {
                Closeables.CloseQuietly(fileInputStream);
            }
        }

        /// <summary>
        /// Checks if byteArray interpreted as sequence of bytes has a subsequence equal to pattern
        /// starting at position equal to offset.
        /// <param name="byteArray"></param>
        /// <param name="offset"></param>
        /// <param name="pattern"></param>
        /// @return true if match succeeds, false otherwise
        /// </summary>
        private static bool MatchBytePattern(
            byte[] byteArray,
            int offset,
            byte[] pattern)
        {
            Preconditions.CheckNotNull(byteArray);
            Preconditions.CheckNotNull(pattern);
            Preconditions.CheckArgument(offset >= 0);
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

        /// <summary>
        /// Helper method that transforms provided string into it's byte representation
        /// using ASCII encoding
        /// <param name="value"></param>
        /// @return byte array representing ascii encoded value
        /// </summary>
        private static byte[] AsciiBytes(string value)
        {
            Preconditions.CheckNotNull(value);

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
        /// Determines type of WebP image. imageHeaderBytes has to be header of a WebP image
        /// </summary>
        private static ImageFormat GetWebpFormat(byte[] imageHeaderBytes, int headerSize)
        {
            Preconditions.CheckArgument(WebpSupportStatus.IsWebpHeader(imageHeaderBytes, 0, headerSize));
            if (WebpSupportStatus.IsSimpleWebpHeader(imageHeaderBytes, 0))
            {
                return ImageFormat.WEBP_SIMPLE;
            }

            if (WebpSupportStatus.IsLosslessWebpHeader(imageHeaderBytes, 0))
            {
                return ImageFormat.WEBP_LOSSLESS;
            }

            if (WebpSupportStatus.IsExtendedWebpHeader(imageHeaderBytes, 0, headerSize))
            {
                if (WebpSupportStatus.IsAnimatedWebpHeader(imageHeaderBytes, 0))
                {
                    return ImageFormat.WEBP_ANIMATED;
                }

                if (WebpSupportStatus.IsExtendedWebpHeaderWithAlpha(imageHeaderBytes, 0))
                {
                    return ImageFormat.WEBP_EXTENDED_WITH_ALPHA;
                }

                return ImageFormat.WEBP_EXTENDED;
            }

            return ImageFormat.UNKNOWN;
        }

        /// <summary>
        /// Checks if imageHeaderBytes starts with SOI (start of image) marker, followed by 0xFF.
        /// If headerSize is lower than 3 false is returned.
        /// Description of jpeg format can be found here:
        /// <a href="http://www.w3.org/Graphics/JPEG/itu-t81.pdf">
        ///   http://www.w3.org/Graphics/JPEG/itu-t81.pdf</a>
        /// Annex B deals with compressed data format
        /// <param name="imageHeaderBytes"></param>
        /// <param name="headerSize"></param>
        /// @return true if imageHeaderBytes starts with SOI_BYTES and headerSize >= 3
        /// </summary>
        private static bool IsJpegHeader(byte[] imageHeaderBytes, int headerSize)
        {
            return headerSize >= JPEG_HEADER.Length && MatchBytePattern(imageHeaderBytes, 0, JPEG_HEADER);
        }

        /// <summary>
        /// Checks if array consisting of first headerSize bytes of imageHeaderBytes
        /// starts with png signature. More information on PNG can be found there:
        /// <a href="http://en.wikipedia.org/wiki/Portable_Network_Graphics">
        ///   http://en.wikipedia.org/wiki/Portable_Network_Graphics</a>
        /// <param name="imageHeaderBytes"></param>
        /// <param name="headerSize"></param>
        /// @return true if imageHeaderBytes starts with PNG_HEADER
        /// </summary>
        private static bool IsPngHeader(byte[] imageHeaderBytes, int headerSize)
        {
            return headerSize >= PNG_HEADER.Length && MatchBytePattern(imageHeaderBytes, 0, PNG_HEADER);
        }

        /// <summary>
        /// Checks if first headerSize bytes of imageHeaderBytes constitute a valid header for a gif image.
        /// Details on GIF header can be found <a href="http://www.w3.org/Graphics/GIF/spec-gif89a.txt">
        ///  on page 7</a>
        /// <param name="imageHeaderBytes"></param>
        /// <param name="headerSize"></param>
        /// @return true if imageHeaderBytes is a valid header for a gif image
        /// </summary>
        private static bool IsGifHeader(byte[] imageHeaderBytes, int headerSize)
        {
            if (headerSize < GIF_HEADER_LENGTH)
            {
                return false;
            }

            return MatchBytePattern(imageHeaderBytes, 0, GIF_HEADER_87A) ||
                MatchBytePattern(imageHeaderBytes, 0, GIF_HEADER_89A);
        }

        /// <summary>
        /// Checks if first headerSize bytes of imageHeaderBytes constitute a valid header for a bmp image.
        /// Details on BMP header can be found <a href="http://www.onicos.com/staff/iz/formats/bmp.html">
        /// </a>
        /// <param name="imageHeaderBytes"></param>
        /// <param name="headerSize"></param>
        /// @return true if imageHeaderBytes is a valid header for a bmp image
        /// </summary>
        private static bool IsBmpHeader(byte[] imageHeaderBytes, int headerSize)
        {
            if (headerSize < BMP_HEADER.Length)
            {
                return false;
            }

            return MatchBytePattern(imageHeaderBytes, 0, BMP_HEADER);
        }
    }
}
