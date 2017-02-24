using ImagePipeline.Common;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Implemented producers can be queried for whether they are likely
    /// to be able to produce a result of the desired size.
    ///
    /// <para />ProduceResults(IConsumer{T}, IProducerContext) may send
    /// a null image to the consumer, even if an image is available, if
    /// the ultimate image is smaller than wanted. This may happen even
    /// if the producer thought it would be able to satisfy the request.
    /// </summary>
    public interface IThumbnailProducer<T> : IProducer<T>
    {
        /// <summary>
        /// Checks whether the producer may be able to produce images of
        /// the specified size. This makes no promise about being able to
        /// produce images for a particular source, only generally being
        /// able to produce output of the desired resolution.
        /// </summary>
        /// <param name="resizeOptions">
        /// The resize options from the current request.
        /// </param>
        /// <returns>
        /// true if the producer can meet these needs.
        /// </returns>
        bool CanProvideImageForSize(ResizeOptions resizeOptions);
    }
}
