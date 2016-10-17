using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace FBCore.Common.Util
{
    /// <summary>
    /// Static methods for secure hashing.
    /// </summary>
    public class SecureHashUtil
    {
        private static readonly byte[] HEX_CHAR_TABLE =
        {
            (byte) '0', (byte) '1', (byte) '2', (byte) '3',
            (byte) '4', (byte) '5', (byte) '6', (byte) '7',
            (byte) '8', (byte) '9', (byte) 'a', (byte) 'b',
            (byte) 'c', (byte) 'd', (byte) 'e', (byte) 'f'
        };

        private static readonly char[] PADDING = { '=' };

        /// <summary>
        /// Make SHA1 hash
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string MakeSHA1Hash(string text)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                return MakeSHA1Hash(bytes);
            }
            catch (EncoderFallbackException e)
            {
                throw e;
            }
            catch (ArgumentNullException e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Generates the SHA1 hash of the input byte array and converts to hex string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>Hex string</returns>
        public static string MakeSHA1Hash(byte[] bytes)
        {
            HashAlgorithmProvider provider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);
            IBuffer sha1HashBuffer = provider.HashData(bytes.AsBuffer());
            return ConvertToHex(sha1HashBuffer.ToArray());
        }

        /// <summary>
        /// Generates the SHA1 hash of the input byte array and encodes to base64 string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>Base64 string</returns>
        public static string MakeSHA1HashBase64(byte[] bytes)
        {
            try
            {
                HashAlgorithmProvider provider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);
                IBuffer sha1HashBuffer = provider.HashData(bytes.AsBuffer());

                // http://stackoverflow.com/questions/26353710/how-to-achieve-base64-url-safe-encoding-in-c
                return CryptographicBuffer
                    .EncodeToBase64String(sha1HashBuffer)
                    .TrimEnd(PADDING)
                    .Replace('+', '-')
                    .Replace('/', '_'); ;
            }
            catch (ArgumentNullException e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Make MD5 hash
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string MakeMD5Hash(string text)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                return MakeMD5Hash(bytes);
            }
            catch (EncoderFallbackException e)
            {
                throw e;
            }
            catch (ArgumentNullException e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Generates the MD5 hash of the input byte array and converts to hex string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>Hex string</returns>
        public static string MakeMD5Hash(byte[] bytes)
        {
            HashAlgorithmProvider provider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer sha1HashBuffer = provider.HashData(bytes.AsBuffer());
            return ConvertToHex(sha1HashBuffer.ToArray());
        }

        /// <summary>
        /// Converts byte array to hex string
        /// </summary>
        /// <param name="raw"></param>
        /// <returns></returns>
        public static string ConvertToHex(byte[] raw)
        {
            StringBuilder sb = new StringBuilder(raw.Length);
            foreach (byte b in raw)
            {
                int v = b & 0xFF;
                sb.Append((char) HEX_CHAR_TABLE[v >> 4]);
                sb.Append((char) HEX_CHAR_TABLE[v & 0xF]);
            }

            return sb.ToString();
        }
    }
}
