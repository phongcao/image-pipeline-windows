using FBCore.Concurrency;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Request;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Executes a local fetch from an asset.
    /// </summary>
    public class LocalAssetFetchProducer : LocalFetchProducer
    {
        internal const string PRODUCER_NAME = "LocalAssetFetchProducer";

        /// <summary>
        /// Instantiates the <see cref="LocalAssetFetchProducer"/>
        /// </summary>
        public LocalAssetFetchProducer(
            IExecutorService executor,
            IPooledByteBufferFactory pooledByteBufferFactory) : base(
                executor,
                pooledByteBufferFactory)
        {
        }

        /// <summary>
        /// Gets an encoded image from the local resource. It can be either backed 
        /// by a FileStream or a PooledByteBuffer
        /// <param name="imageRequest">Request that includes the local resource that 
        /// is being accessed</param>
        /// @throws IOException
        /// </summary>
        protected override EncodedImage GetEncodedImage(ImageRequest imageRequest)
        {
            var readFileTask = StorageFile.GetFileFromApplicationUriAsync(imageRequest.SourceUri)
                .AsTask()
                .ContinueWith(
                file =>
                {
                    return file.Result.OpenReadAsync().AsTask();
                },
                TaskContinuationOptions.ExecuteSynchronously)
                .Unwrap();

            var fileStream = readFileTask.GetAwaiter().GetResult().AsStream();
            return GetEncodedImage(fileStream, (int)fileStream.Length);
        }

        /// <summary>
        /// The name of the Producer
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
