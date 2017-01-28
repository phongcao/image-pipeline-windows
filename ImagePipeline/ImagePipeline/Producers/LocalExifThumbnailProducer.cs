using FBCore.Concurrency;
using ImagePipeline.Common;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// A producer that retrieves exif thumbnails.
    ///
    /// <para />At present, these thumbnails are retrieved on the java heap before being 
    /// put into native memory.
    /// </summary>
    public class LocalExifThumbnailProducer : IThumbnailProducer<EncodedImage>
    {
        internal const string PRODUCER_NAME = "LocalExifThumbnailProducer";

        /// <summary>
        /// Instantiates the <see cref="LocalExifThumbnailProducer"/>
        /// </summary>
        public LocalExifThumbnailProducer(
            IExecutorService executor,
            IPooledByteBufferFactory pooledByteBufferFactory)
        {
        }

        /// <summary>
        /// Checks whether the producer may be able to produce images of the specified 
        /// size. This makes no promise about being able to produce images for a particular 
        /// source, only generally being able to produce output of the desired resolution.
        ///
        /// <para /> In this case, assumptions are made about the common size of EXIF 
        /// thumbnails which is that they may be up to 512 pixels in each dimension.
        ///
        /// <param name="resizeOptions">The resize options from the current request.</param>
        /// @return true if the producer can meet these needs.
       /// </summary>
        public bool CanProvideImageForSize(ResizeOptions resizeOptions)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified 
        /// whenever progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
          IConsumer<EncodedImage> consumer,
          IProducerContext producerContext)
        {
            throw new NotImplementedException();
        }
    }
}
