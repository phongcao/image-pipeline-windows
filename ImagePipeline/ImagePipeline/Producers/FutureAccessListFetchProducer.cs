using FBCore.Concurrency;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Request;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.AccessCache;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Represents a future-access list fetch producer.
    /// </summary>
    public class FutureAccessListFetchProducer : LocalFetchProducer
    {
        internal const string PRODUCER_NAME = "FutureAccessListFetchProducer";

        /// <summary>
        /// Instantiates the <see cref="FutureAccessListFetchProducer"/>.
        /// </summary>
        public FutureAccessListFetchProducer(
            IExecutorService executor,
            IPooledByteBufferFactory pooledByteBufferFactory) : base(
                executor,
                pooledByteBufferFactory)
        {
        }

        /// <summary>
        /// Gets the encoded image.
        /// </summary>
        protected override Task<EncodedImage> GetEncodedImage(ImageRequest imageRequest)
        {
            string originalString = imageRequest.SourceUri.OriginalString;
            string token = originalString.Substring(originalString.LastIndexOf(":") + 1);
            return StorageApplicationPermissions.FutureAccessList.GetFileAsync(token).AsTask()
                .ContinueWith(
                (filePathTask) =>
                {
                    return filePathTask.Result.OpenStreamForReadAsync();
                })
                .Unwrap()
                .ContinueWith(
                (fileReadTask) =>
                {
                    var stream = fileReadTask.Result;
                    return GetEncodedImage(stream, (int)(stream.Length));
                });
        }

        /// <summary>
        /// The name of the Producer.
        /// </summary>
        protected override string ProducerName
        {
            get
            {
                return PRODUCER_NAME;
            }
        }
    }
}
