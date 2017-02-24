using ImagePipeline.Request;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Used to pass context information to producers.
    ///
    /// <para />Object implementing this interface is passed to all producers
    /// participating in pipeline request <see cref="IProducer{T}.ProduceResults"/>. 
    /// Its responsibility is to instruct producers which image should be 
    /// fetched/decoded/resized/cached etc.
    /// This class also handles request cancellation.
    /// </summary>
    public interface IProducerContext
    {
        /// <summary>
        /// Gets the image request.
        /// </summary>
        /// <returns>Image request that is being executed.</returns>
        ImageRequest ImageRequest { get; }

        /// <summary>
        /// Gets the request id.
        /// </summary>
        /// <returns>Id of this request.</returns>
        string Id { get; }

        /// <summary>
        /// Gets the producer listener.
        /// </summary>
        /// <returns>
        /// IProducerListener for producer's events.
        /// </returns>
        IProducerListener Listener { get; }

        /// <summary>
        /// Gets the caller context.
        /// </summary>
        /// <returns>
        /// The <see cref="object"/> that indicates the caller's context.
        /// </returns>
        object CallerContext { get; }

        /// <summary>
        /// Gets the lowest permitted request level.
        /// </summary>
        /// <returns>
        /// The lowest permitted <see cref="RequestLevel"/>.
        /// </returns>
        int LowestPermittedRequestLevel { get; }

        /// <summary>
        /// Checks if the request is a prefetch.
        /// </summary>
        /// <returns>
        /// true if the request is a prefetch, false otherwise.
        /// </returns>
        bool IsPrefetch { get; }

        /// <summary>
        /// Gets the priority of the request.
        /// </summary>
        /// <returns>Priority of the request.</returns>
        int Priority { get; }

        /// <summary>
        /// Checks if request's owner expects intermediate results.
        /// </summary>
        /// <returns>
        /// true if request's owner expects intermediate results.
        /// </returns>
        bool IsIntermediateResultExpected { get; }

        /// <summary>
        /// Adds callbacks to the set of callbacks that are executed at
        /// various points during the processing of a request.
        /// </summary>
        /// <param name="callbacks">Callbacks to be executed.</param>
        void AddCallbacks(IProducerContextCallbacks callbacks);
    }
}
