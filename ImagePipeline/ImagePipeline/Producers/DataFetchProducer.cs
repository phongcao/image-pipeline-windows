using FBCore.Common.Internal;
using FBCore.Concurrency;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Request;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Producer for data URIs.
    ///
    /// <para />Data URIs embed the data in the URI itself. They don't point to a 
    /// file location; the URI is the data. Data can be encoded in either base-64 
    /// or escaped ASCII. See the <a href="http://tools.ietf.org/html/rfc2397">spec</a> 
    /// for full details.
    ///
    /// <para />Data URIs are intended for small pieces of data only, since the URI 
    /// lives on the heap. For large data, use a another URI type.
    ///
    /// <para />Charsets specified in the URI are ignored. Only UTF-8 encoding is 
    /// currently supported.
    /// </summary>
    public class DataFetchProducer : LocalFetchProducer
    {
        private const string PRODUCER_NAME = "DataFetchProducer";

        /// <summary>
        /// Instantiates the <see cref="DataFetchProducer"/>
        /// </summary>
        public DataFetchProducer(
            IPooledByteBufferFactory pooledByteBufferFactory,
            bool fileDescriptorEnabled) : base(
                CallerThreadExecutor.Instance,
                pooledByteBufferFactory)
        {
        }

        /// <summary>
        /// Gets the encoded image.
        /// </summary>
        protected override EncodedImage GetEncodedImage(ImageRequest imageRequest)
        {
            byte[] data = GetData(imageRequest.SourceUri.ToString());
            return GetByteBufferBackedEncodedImage(new MemoryStream(data), data.Length);
        }

        /// <summary>
        /// The producer name.
        /// </summary>
        protected override string ProducerName
        {
            get
            {
                return PRODUCER_NAME;
            }
        }

        internal static byte[] GetData(string uri)
        {
            // Format of a data URL:
            // data:mime/type;param=value;param=value;base64,actual_data
            // everything is optional except the actual data, which is either
            // base-64 or escaped ASCII encoded.
            Preconditions.CheckArgument(uri.Substring(0, 5).Equals("data:"));
            int commaPos = uri.IndexOf(',');

            string dataStr = uri.Substring(commaPos + 1, uri.Length);
            if (IsBase64(uri.Substring(0, commaPos)))
            {
                return Convert.FromBase64String(dataStr);
            }
            else
            {
                string str = WebUtility.UrlDecode(dataStr);
                byte[] b = Encoding.UTF8.GetBytes(str);
                return b;
            }
        }

        internal static bool IsBase64(string prefix)
        {
            if (!prefix.Contains(";"))
            {
                return false;
            }

            string[] parameters = prefix.Split(';');
            return parameters[parameters.Length - 1].Equals("base64");
        }
    }
}
