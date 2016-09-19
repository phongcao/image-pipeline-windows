using FBCore.Common.Util;
using System.Text;

namespace ImagePipelineBase.Tests.ImageUtils
{
    /// <summary>
    /// Provide test use functions for JfifUtilTest.
    /// </summary>
    public class JfifTestUtils
    {
        /// <summary>
        /// Marker
        /// </summary>
        public const string SOI = "FFD8";

        /// <summary>
        /// Marker
        /// </summary>
        public const string DQT_MARKER = "FFDB";

        /// <summary>
        /// Marker
        /// </summary>
        public const string DHT_MARKER = "FFC4";

        /// <summary>
        /// Marker
        /// </summary>
        public const string DRI_MARKER = "FFDD";

        /// <summary>
        /// Marker
        /// </summary>
        public const string SOF_MARKER = "FFC0";

        /// <summary>
        /// Marker
        /// </summary>
        public const string SOS_MARKER = "FFDA";

        /// <summary>
        /// Marker
        /// </summary>
        public const string EOI = "FFD9";

        /// <summary>
        /// Marker
        /// </summary>
        public const string APP0_MARKER = "FFE0";

        /// <summary>
        /// Marker
        /// </summary>
        public const string APP1_MARKER = "FFE1";

        /// <summary>
        /// Marker
        /// </summary>
        public const string APP2_MARKER = "FFE2";

        /// <summary>
        /// content length 4
        /// </summary>
        public const string DQT = DQT_MARKER + "0004 0000";

        /// <summary>
        /// content length 6
        /// </summary>
        public const string DHT = DHT_MARKER + "0006 0000 0000";

        /// <summary>
        /// content length 4, optional
        /// </summary>
        public const string DRI = DRI_MARKER + "0004 0000";

        /// <summary>
        /// content length 6
        /// </summary>
        public const string SOF = SOF_MARKER + "0006 0000 0000";

        /// <summary>
        /// content length 4
        /// </summary>
        public const string SOS = SOS_MARKER + "0004 0000";

        /// <summary>
        /// content length 4, optional
        /// </summary>
        public const string APP0 = APP0_MARKER + "0004 0000";

        /// <summary>
        /// content length 4, optional
        /// </summary>
        public const string APP2 = APP2_MARKER + "0004 0000";

        /// <summary>
        /// APP1 related headers and magic number.
        /// </summary>
        public const string APP1_EXIF_MAGIC = "4578 6966 0000";

        /// <summary>
        /// APP1 related headers and magic number.
        /// </summary>
        public const string TIFF_HEADER_LE = "4949 2A00 0800 0000";

        /// <summary>
        /// APP1 related headers and magic number.
        /// </summary>
        public const string TIFF_HEADER_BE = "4D4D 002A 0000 0008";

        /// <summary>
        /// IFD related content constant definition
        /// </summary>
        public const int IFD_ENTRY_ORI_TAG = 0x0112;

        /// <summary>
        /// IFD related content constant definition
        /// </summary>
        public const int IFD_ENTRY_TAG_1 = 0x011A;

        /// <summary>
        /// IFD related content constant definition
        /// </summary>
        public const int IFD_ENTRY_TAG_2 = 0x011B;

        /// <summary>
        /// IFD related content constant definition
        /// </summary>
        public const int IFD_ENTRY_TAG_3 = 0x011C;

        /// <summary>
        /// IFD related content constant definition
        /// </summary>
        public const int TYPE_SHORT = 3;

        /// <summary>
        /// Gets the number of bytes from TIFF string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int NumBytes(string data)
        {
            return data.Replace(" ", "").Length / 2;
        }

        /// <summary>
        /// Converts hex string to byte array
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] HexStringToByteArray(string s)
        {
            string noSpaceString = s.Replace(" ", "");
            byte[] data = Hex.DecodeHex(noSpaceString);
            return data;
        }

        /// <summary>
        /// Encodes int to hex string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="length"></param>
        /// <param name="littleEndian"></param>
        /// <returns></returns>
        public static string EncodeInt2HexString(int value, int length, bool littleEndian)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                int oneByte = value & 0xFF;
                if (littleEndian)
                {
                    sb.Append(Hex.Byte2Hex(oneByte));
                }
                else
                {
                    sb.Insert(0, Hex.Byte2Hex(oneByte));
                }

                value = value >> 8;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Makes SOF section
        /// </summary>
        /// <param name="length"></param>
        /// <param name="bitDepth"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static string MakeSOFSection(int length, int bitDepth, int width, int height)
        {
            return SOF_MARKER + EncodeInt2HexString(length, 2, false) +
                EncodeInt2HexString(bitDepth, 1, false) +
                EncodeInt2HexString(height, 2, false) +
                EncodeInt2HexString(width, 2, false) +
                EncodeInt2HexString(0, length - 7, false);
        }

        /// <summary>
        /// Makes orientation entry
        /// </summary>
        /// <param name="orientation"></param>
        /// <param name="isLittleEnd"></param>
        /// <returns></returns>
        public static string MakeOrientationEntry(int orientation, bool isLittleEnd)
        {
            return MakeIfdEntry(
                IFD_ENTRY_ORI_TAG, TYPE_SHORT, 1, orientation, 2, isLittleEnd);
        }

        /// <summary>
        /// Makes Ifd entry
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="type"></param>
        /// <param name="count"></param>
        /// <param name="value"></param>
        /// <param name="valueNumBytes"></param>
        /// <param name="littleEndian"></param>
        /// <returns></returns>
        public static string MakeIfdEntry(
            int tag,
            int type,
            int count,
            int value,
            int valueNumBytes,
            bool littleEndian)
        {
            return EncodeInt2HexString(tag, 2, littleEndian) +
                EncodeInt2HexString(type, 2, littleEndian) +
                EncodeInt2HexString(count, 4, littleEndian) +
                EncodeInt2HexString(value, valueNumBytes, littleEndian) +
                EncodeInt2HexString(0, 4 - valueNumBytes, littleEndian);
        }

        /// <summary>
        /// Makes Ifd
        /// </summary>
        /// <param name="IfdEntries"></param>
        /// <param name="nextEntryOffset"></param>
        /// <param name="littleEndian"></param>
        /// <returns></returns>
        public static string MakeIfd(string[] IfdEntries, int nextEntryOffset, bool littleEndian)
        {
            string ret = EncodeInt2HexString(IfdEntries.Length, 2, littleEndian);
            for (int i = 0; i < IfdEntries.Length; i++)
            {
                ret += IfdEntries[i];
            }

            ret += EncodeInt2HexString(nextEntryOffset, 4, littleEndian);
            return ret;
        }

        /// <summary>
        /// Makes TIFF
        /// </summary>
        /// <param name="ifd"></param>
        /// <param name="littleEndian"></param>
        /// <returns></returns>
        public static string MakeTiff(string ifd, bool littleEndian)
        {
            string ret = littleEndian ? TIFF_HEADER_LE : TIFF_HEADER_BE;
            return ret + ifd;
        }

        /// <summary>
        /// Makes APP1_EXIF
        /// </summary>
        /// <param name="tiff"></param>
        /// <returns></returns>
        public static string MakeAPP1_EXIF(string tiff)
        {
            string app1Length = EncodeInt2HexString(NumBytes(tiff) + 8, 2, false);
            return APP1_MARKER + app1Length + APP1_EXIF_MAGIC + tiff;
        }

        /// <summary>
        /// Makes test image with APP1
        /// </summary>
        /// <param name="APP1"></param>
        /// <returns></returns>
        public static string MakeTestImageWithAPP1(string APP1)
        {
            return SOI + APP0 + APP1 + DQT + DHT + SOF + SOS + EOI;
        }
    }
}
