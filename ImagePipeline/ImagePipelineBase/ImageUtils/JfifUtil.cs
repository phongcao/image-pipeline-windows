using FBCore.Common.Internal;
using System.IO;

namespace ImageUtils
{
    /// <summary>
    /// Util for obtaining information from JPEG file.
    /// </summary>
    public class JfifUtil
    {
        /// <summary>
        /// Definitions of jpeg markers as well as overall description of jpeg
        /// file format can be found here:
        /// <a href="http://www.w3.org/Graphics/JPEG/itu-t81.pdf">
        /// Recommendation T.81.
        /// </a>
        /// </summary>
        public const int MARKER_FIRST_BYTE = 0xFF;

        /// <summary>
        /// Escape.
        /// </summary>
        public const int MARKER_ESCAPE_BYTE = 0x00;

        /// <summary>
        /// Start of image marker.
        /// </summary>
        public const int MARKER_SOI = 0xD8;

        /// <summary>
        /// Temporary marker.
        /// </summary>
        public const int MARKER_TEM = 0x01;

        /// <summary>
        /// End of image marker.
        /// </summary>
        public const int MARKER_EOI = 0xD9;

        /// <summary>
        /// Start of scan.
        /// </summary>
        public const int MARKER_SOS = 0xDA;

        /// <summary>
        /// Marker reserved for application segments.
        /// </summary>
        public const int MARKER_APP1 = 0xE1;

        /// <summary>
        /// Baseline DCT process frame marker.
        /// </summary>
        public const int MARKER_SOFn = 0xC0;

        /// <summary>
        /// RST.
        /// </summary>
        public const int MARKER_RST0 = 0xD0;

        /// <summary>
        /// RST.
        /// </summary>
        public const int MARKER_RST7 = 0xD7;

        /// <summary>
        /// EXIF.
        /// </summary>
        public const int APP1_EXIF_MAGIC = 0x45786966;

        private JfifUtil()
        {
        }

        /// <summary>
        /// Determines auto-rotate angle based on orientation information.
        /// </summary>
        /// <param name="orientation">
        /// Orientation information, one of {1, 3, 6, 8}.
        /// </param>
        /// <returns>
        /// Orientation: 1/3/6/8 -> 0/180/90/270.
        /// </returns>
        public static int GetAutoRotateAngleFromOrientation(int orientation)
        {
            return TiffUtil.GetAutoRotateAngleFromOrientation(orientation);
        }

        /// <summary>
        /// Gets orientation information from jpeg byte array.
        /// </summary>
        /// <param name="jpeg">
        /// The input byte array of jpeg image.
        /// </param>
        /// <returns>
        /// Orientation: 1/8/3/6.
        /// Returns 0 if there is no valid orientation information.
        /// </returns>
        public static int GetOrientation(byte[] jpeg)
        {
            // Wrapping with ByteArrayInputStream is cheap and we don't
            // have duplicate implementation
            return GetOrientation(new MemoryStream(jpeg));
        }

        /// <summary>
        /// Get orientation information from jpeg input stream.
        /// </summary>
        /// <param name="inputStream">
        /// The input stream of jpeg image.
        /// </param>
        /// <returns>
        /// Orientation: 1/8/3/6.
        /// Returns 0 if there is no valid orientation information.
        /// </returns>
        public static int GetOrientation(Stream inputStream)
        {
            try
            {
                int length = MoveToAPP1EXIF(inputStream);
                if (length == 0)
                {
                    return 0; // unknown orientation
                }

                return TiffUtil.ReadOrientationFromTIFF(inputStream, length);
            }
            catch (IOException)
            {
                return 0;
            }
        }

        /// <summary>
        /// Reads the content of the input stream until specified marker is found.
        /// Marker will be consumed and the input stream will be positioned after
        /// the specified marker.
        /// </summary>
        /// <param name="inputStream">The input stream to read bytes from.</param>
        /// <param name="markerToFind">The marker we are looking for.</param>
        /// <returns>
        /// bool: whether or not we found the expected marker from input stream.
        /// </returns>
        public static bool MoveToMarker(Stream inputStream, int markerToFind)
        {
            Preconditions.CheckNotNull(inputStream);

            // ISO/IEC 10918-1:1993(E)
            while (StreamProcessor.ReadPackedInt(inputStream, 1, false) == MARKER_FIRST_BYTE)
            {
                int marker = MARKER_FIRST_BYTE;
                while (marker == MARKER_FIRST_BYTE)
                {
                    marker = StreamProcessor.ReadPackedInt(inputStream, 1, false);
                }

                if (markerToFind == MARKER_SOFn && IsSOFn(marker))
                {
                    return true;
                }

                if (marker == markerToFind)
                {
                    return true;
                }

                // Check if the marker is SOI or TEM. These two don't have length
                // field, so we skip it.
                if (marker == MARKER_SOI || marker == MARKER_TEM)
                {
                    continue;
                }

                // Check if the marker is EOI or SOS. We will stop reading since
                // metadata markers don't come after these two markers.
                if (marker == MARKER_EOI || marker == MARKER_SOS)
                {
                    return false;
                }

                // read block length
                // subtract 2 as length contain SIZE field we just read
                int length = StreamProcessor.ReadPackedInt(inputStream, 2, false) - 2;

                // Skip other markers.
                inputStream.Seek(length, SeekOrigin.Current);
            }

            return false;
        }

        private static bool IsSOFn(int marker)
        {
            // There are no SOF4, SOF8, SOF12
            switch (marker)
            {
                case 0xC0:
                case 0xC1:
                case 0xC2:
                case 0xC3:
                case 0xC5:
                case 0xC6:
                case 0xC7:
                case 0xC9:
                case 0xCA:
                case 0xCB:
                case 0xCD:
                case 0xCE:
                case 0xCF:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Positions the given input stream to the beginning of the EXIF data in
        /// the JPEG APP1 block.
        /// </summary>
        /// <param name="inputStream">The input stream of jpeg image.</param>
        /// <returns>Length of EXIF data.</returns>
        private static int MoveToAPP1EXIF(Stream inputStream)
        {
            if (MoveToMarker(inputStream, MARKER_APP1))
            {
                // Read block length
                // subtract 2 as length contain SIZE field we just read
                int length = StreamProcessor.ReadPackedInt(inputStream, 2, false) - 2;
                if (length > 6)
                {
                    int magic = StreamProcessor.ReadPackedInt(inputStream, 4, false);
                    length -= 4;
                    int zero = StreamProcessor.ReadPackedInt(inputStream, 2, false);
                    length -= 2;
                    if (magic == APP1_EXIF_MAGIC && zero == 0)
                    {
                        // JEITA CP-3451 Exif Version 2.2
                        return length;
                    }
                }
            }

            return 0;
        }
    }
}
