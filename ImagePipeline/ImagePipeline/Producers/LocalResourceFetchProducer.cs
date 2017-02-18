using FBCore.Concurrency;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Request;
using System;
using System.Threading.Tasks;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Executes a local fetch from a resource.
    /// </summary>
    public class LocalResourceFetchProducer : LocalFetchProducer
    {
        internal const string PRODUCER_NAME = "LocalResourceFetchProducer";

        /// <summary>
        /// Instantiates the <see cref="LocalResourceFetchProducer"/>
        /// </summary>
        public LocalResourceFetchProducer(
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
            throw new NotImplementedException();
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
