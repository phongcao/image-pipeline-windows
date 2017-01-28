using ImagePipeline.Common;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Implemented producers can be queried for whether they are likely to be able to 
    /// produce a result of the desired size.
    ///
    /// <para /> ProduceResults(IConsumer{T}, IProducerContext) may send a null image 
    /// to the consumer, even if an image is available, if the ultimate image is smaller 
    /// than wanted. This may happen even if the producer thought it would be able to 
    /// satisfy the request.
    /// </summary>
    public interface IThumbnailProducer<T> : IProducer<T>
    {
        /// <summary>
        /// Checks whether the producer may be able to produce images of the specified 
        /// size. This makes no promise about being able to produce images for a particular 
        /// source, only generally being able to produce output of the desired resolution.
        ///
        /// <param name="resizeOptions">the resize options from the current request</param>
        /// @return true if the producer can meet these needs
        /// </summary>
        bool CanProvideImageForSize(ResizeOptions resizeOptions);
    }
}
