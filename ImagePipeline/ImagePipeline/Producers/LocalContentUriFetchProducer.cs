using FBCore.Concurrency;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Request;
using System;
using System.Threading.Tasks;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Represents a local content Uri fetch producer.
    /// </summary>
    public class LocalContentUriFetchProducer : LocalFetchProducer
    {
        internal const string PRODUCER_NAME = "LocalContentUriFetchProducer";

        /// <summary>
        /// Instantiates the <see cref="LocalContentUriFetchProducer"/>.
        /// </summary>
        public LocalContentUriFetchProducer(
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
