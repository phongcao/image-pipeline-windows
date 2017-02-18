using FBCore.Concurrency;
using ImagePipeline.Common;
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
    public class LocalContentUriThumbnailFetchProducer : 
        LocalFetchProducer, IThumbnailProducer<EncodedImage>
    {
        internal const string PRODUCER_NAME = "LocalContentUriThumbnailFetchProducer";

        /// <summary>
        /// Instantiates the <see cref="LocalContentUriThumbnailFetchProducer"/>
        /// </summary>
        public LocalContentUriThumbnailFetchProducer(
            IExecutorService executor,
            IPooledByteBufferFactory pooledByteBufferFactory) : base(
                executor,
                pooledByteBufferFactory)
        {
        }

        /// <summary>
        /// Checks whether the producer may be able to produce images of the specified 
        /// size. This makes no promise about being able to produce images for a particular 
        /// source, only generally being able to produce output of the desired resolution.
        ///
        /// <param name="resizeOptions">the resize options from the current request</param>
        /// @return true if the producer can meet these needs
        /// </summary>
        public bool CanProvideImageForSize(ResizeOptions resizeOptions)
        {
            throw new NotImplementedException();
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
