using System.Diagnostics;
using System.IO;

namespace ImageUtils
{
    /// <summary>
    /// Util for getting exif orientation from a jpeg stored as a byte array.
    /// </summary>
    class TiffUtil
    {
        public const int TIFF_BYTE_ORDER_BIG_END = 0x4D4D002A;
        public const int TIFF_BYTE_ORDER_LITTLE_END = 0x49492A00;
        public const int TIFF_TAG_ORIENTATION = 0x0112;
        public const int TIFF_TYPE_SHORT = 3;

        /// <summary>
        /// Determines auto-rotate angle based on orientation information.
        /// <param name="orientation">orientation information read from APP1 EXIF (TIFF) block.</param>
        /// @return orientation: 1/3/6/8 -> 0/180/90/270.
        /// </summary>
        public static int GetAutoRotateAngleFromOrientation(int orientation)
        {
            switch (orientation)
            {
                case ExifInterface.ORIENTATION_NORMAL:
                    return 0;
                case ExifInterface.ORIENTATION_ROTATE_180:
                    return 180;
                case ExifInterface.ORIENTATION_ROTATE_90:
                    return 90;
                case ExifInterface.ORIENTATION_ROTATE_270:
                    return 270;
            }

            Debug.WriteLine("Unsupported orientation");
            return 0;
        }

        /// <summary>
        /// Reads orientation information from TIFF data.
        /// <param name="inputStream">the input stream of TIFF data</param>
        /// <param name="length">length of the TIFF data</param>
        /// @return orientation information (1/3/6/8 on success, 0 if not found)
        /// </summary>
        public static int ReadOrientationFromTIFF(Stream inputStream, int length)
        {
            // Read tiff header
            TiffHeader tiffHeader = new TiffHeader();
            length = ReadTiffHeader(inputStream, length, tiffHeader);

            // Move to the first IFD
            // offset is relative to the beginning of the TIFF data
            // and we already consumed the first 8 bytes of header
            int toSkip = tiffHeader.FirstIfdOffset - 8;
            if (length == 0 || toSkip > length)
            {
                return 0;
            }

            inputStream.Seek(toSkip, SeekOrigin.Current);
            length -= toSkip;

            // Move to the entry with orientation tag
            length = MoveToTiffEntryWithTag(inputStream, length, tiffHeader.LittleEndian, TIFF_TAG_ORIENTATION);

            // Read orientation
            return GetOrientationFromTiffEntry(inputStream, length, tiffHeader.LittleEndian);
        }

        /// <summary>
        /// Structure that holds TIFF header.
        /// </summary>
        class TiffHeader
        {
            internal bool LittleEndian { get; set; }

            internal int ByteOrder { get; set; }

            internal int FirstIfdOffset { get; set; }
        }

        /// <summary>
        /// Reads the TIFF header to the provided structure.
        /// <param name="inputStream">the input stream of TIFF data</param>
        /// <param name="length">length of the TIFF data</param>
        /// <param name="tiffHeader">TIFF header</param>
        /// @return remaining length of the data on success, 0 on failure
        /// @throws IOException
        /// </summary>
        private static int ReadTiffHeader(Stream inputStream, int length, TiffHeader tiffHeader)
        {
            if (length <= 8)
            {
                return 0;
            }

            // Read the byte order
            tiffHeader.ByteOrder = StreamProcessor.ReadPackedInt(inputStream, 4, false);
            length -= 4;
            if (tiffHeader.ByteOrder != TIFF_BYTE_ORDER_LITTLE_END && 
                tiffHeader.ByteOrder != TIFF_BYTE_ORDER_BIG_END)
            {
                Debug.WriteLine("Invalid TIFF header");
                return 0;
            }

            tiffHeader.LittleEndian = (tiffHeader.ByteOrder == TIFF_BYTE_ORDER_LITTLE_END);

            // Read the offset of the first IFD and check if it is reasonable
            tiffHeader.FirstIfdOffset = StreamProcessor.ReadPackedInt(inputStream, 4, tiffHeader.LittleEndian);
            length -= 4;
            if (tiffHeader.FirstIfdOffset < 8 || tiffHeader.FirstIfdOffset - 8 > length)
            {
                Debug.WriteLine("Invalid offset");
                return 0;
            }

            return length;
        }

        /// <summary>
        /// Positions the given input stream to the entry that has a specified tag. Tag will be consumed.
        /// <param name="inputStream">the input stream of TIFF data positioned to the beginning of an IFD.</param>
        /// <param name="length">length of the available data in the given input stream.</param>
        /// <param name="isLittleEndian">whether the TIFF data is stored in little or big endian format</param>
        /// <param name="tagToFind">tag to find</param>
        /// @return remaining length of the data on success, 0 on failure
        /// </summary>
        private static int MoveToTiffEntryWithTag(
            Stream inputStream,
            int length,
            bool isLittleEndian,
            int tagToFind)
        {
            if (length < 14)
            {
                return 0;
            }

            // Read the number of entries and go through all of them
            // each IFD entry has length of 12 bytes and is composed of
            // {TAG [2], TYPE [2], COUNT [4], VALUE/OFFSET [4]}
            int numEntries = StreamProcessor.ReadPackedInt(inputStream, 2, isLittleEndian);
            length -= 2;
            while (numEntries-- > 0 && length >= 12)
            {
                int tag = StreamProcessor.ReadPackedInt(inputStream, 2, isLittleEndian);
                length -= 2;
                if (tag == tagToFind)
                {
                    return length;
                }

                inputStream.Seek(10, SeekOrigin.Current);
                length -= 10;
            }

            return 0;
        }

        /// <summary>
        /// Reads the orientation information from the TIFF entry.
        /// It is assumed that the entry has a TIFF orientation tag and that tag has already been consumed.
        /// <param name="inputStream">the input stream positioned at the TIFF entry with tag already being consumed</param>
        /// <param name="isLittleEndian">whether the TIFF data is stored in little or big endian format</param>
        /// <param name="length">length</param>
        /// @return Orientation value in TIFF IFD entry.
        /// </summary>
        private static int GetOrientationFromTiffEntry(Stream inputStream, int length, bool isLittleEndian)
        {
            if (length < 10)
            {
                return 0;
            }

            // Orientation entry has type = short
            int type = StreamProcessor.ReadPackedInt(inputStream, 2, isLittleEndian);
            if (type != TIFF_TYPE_SHORT)
            {
                return 0;
            }

            // Orientation entry has count = 1
            int count = StreamProcessor.ReadPackedInt(inputStream, 4, isLittleEndian);
            if (count != 1)
            {
                return 0;
            }

            int value = StreamProcessor.ReadPackedInt(inputStream, 2, isLittleEndian);
            int padding = StreamProcessor.ReadPackedInt(inputStream, 2, isLittleEndian);
            return value;
        }
    }
}
