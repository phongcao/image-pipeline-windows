using System;

namespace FBCore.Common.Util
{
    /// <summary>
    /// Basic hex operations: from byte array to string and vice versa.
    /// <p/>
    /// TODO: move to the framework and consider implementing as native code.
    /// </summary>
    public class Hex
    {
        private static readonly char[] HEX_DIGITS = new char[]
        {
            '0', '1', '2', '3', '4', '5', '6', '7',
            '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
        };

        private static readonly char[] FIRST_CHAR = new char[256];
        private static readonly char[] SECOND_CHAR = new char[256];
        private static readonly sbyte[] DIGITS = new sbyte['f' + 1];

        static Hex()
        {
            for (int i = 0; i < 256; i++)
            {
                FIRST_CHAR[i] = HEX_DIGITS[(i >> 4) & 0xF];
                SECOND_CHAR[i] = HEX_DIGITS[i & 0xF];
            }

            for (int i = 0; i <= 'F'; i++)
            {
                DIGITS[i] = -1;
            }

            for (sbyte i = 0; i < 10; i++)
            {
                DIGITS['0' + i] = i;
            }

            for (sbyte i = 0; i < 6; i++)
            {
                DIGITS['A' + i] = (sbyte)(10 + i);
                DIGITS['a' + i] = (sbyte)(10 + i);
            }
        }

        /// <summary>
       /// Convert an int [0-255] to a hexadecimal string representation.
       ///
       /// <param name="value">int value.</param>
       /// </summary>
        public static string Byte2Hex(int value)
        {
            if (value > 255 || value < 0)
            {
                throw new ArgumentException("The int converting to hex should be in range 0~255");
            }

            return FIRST_CHAR[value].ToString() + (SECOND_CHAR[value]).ToString();
        }

        /// <summary>
        /// Quickly converts a byte array to a hexadecimal string representation.
        /// </summary>
        public static string EncodeHex(byte[] array, bool zeroTerminated)
        {
            char[] cArray = new char[array.Length * 2];

            int j = 0;
            for (int i = 0; i < array.Length; i++)
            {
                int index = array[i] & 0xFF;
                if (index == 0 && zeroTerminated)
                {
                    break;
                }

                cArray[j++] = FIRST_CHAR[index];
                cArray[j++] = SECOND_CHAR[index];
            }

            return new string(cArray, 0, j);
        }

        /// <summary>
        /// Quickly converts a hexadecimal string to a byte array.
        /// </summary>
        public static byte[] DecodeHex(string hexString)
        {
            int length = hexString.Length;

            if ((length & 0x01) != 0)
            {
                throw new ArgumentException("Odd number of characters.");
            }

            bool badHex = false;
            byte[] outBuf = new byte[length >> 1];
            for (int i = 0, j = 0; j < length; i++)
            {
                int c1 = hexString[j++];
                if (c1 > 'f')
                {
                    badHex = true;
                    break;
                }

                sbyte d1 = DIGITS[c1];
                if (d1 == -1)
                {
                    badHex = true;
                    break;
                }

                int c2 = hexString[j++];
                if (c2 > 'f')
                {
                    badHex = true;
                    break;
                }

                sbyte d2 = DIGITS[c2];
                if (d2 == -1)
                {
                    badHex = true;
                    break;
                }

                outBuf[i] = (byte)((byte)d1 << 4 | (byte)d2);
            }

            if (badHex)
            {
              throw new ArgumentException("Invalid hexadecimal digit: " + hexString);
            }

            return outBuf;
        }

        /// <summary>
        /// Converts hex string to byte array
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] HexStringToByteArray(string s)
        {
            string noSpaceString = s.Replace(" ", "");
            byte[] data = DecodeHex(noSpaceString);
            return data;
        }
    }
}
