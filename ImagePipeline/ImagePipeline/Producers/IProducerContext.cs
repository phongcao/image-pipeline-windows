using ImagePipeline.Common;
using ImagePipeline.Request;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Used to pass context information to producers.
    ///
    /// <para /> Object implementing this interface is passed to all producers participating in pipeline
    /// request {@see Producer.ProduceResults}. Its responsibility is to instruct producers which image
    /// should be fetched/decoded/resized/cached etc. This class also handles request cancellation.
    ///
    /// <para />  In order to be notified when cancellation is requested, a producer should use the
    /// <code> RunOnCancellationRequested</code> method which takes an instance of Runnable and executes 
    /// it when the pipeline client cancels the image request.
    /// </summary>
    public interface IProducerContext
    {
        /// <summary>
        /// @return image request that is being executed
        /// </summary>
        ImageRequest ImageRequest { get; }

        /// <summary>
        /// @return id of this request
        /// </summary>
        string Id { get; }

        /// <summary>
        /// @return ProducerListener for producer's events
        /// </summary>
        IProducerListener Listener { get; }

        /// <summary>
        /// @return the <see cref="object"/> that indicates the caller's context
        /// </summary>
        object CallerContext { get; }

        /// <summary>
        /// @return the lowest permitted <see cref="RequestLevel"/>
        /// </summary>
        int LowestPermittedRequestLevel { get; }

        /// <summary>
        /// @return true if the request is a prefetch, false otherwise.
        /// </summary>
        bool IsPrefetch { get; }

        /// <summary>
        /// @return priority of the request.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// @return true if request's owner expects intermediate results
        /// </summary>
        bool IsIntermediateResultExpected { get; }

        /// <summary>
        /// Adds callbacks to the set of callbacks that are executed at various points during the
        /// processing of a request.
        /// <param name="callbacks">callbacks to be executed</param>
        /// </summary>
        void AddCallbacks(IProducerContextCallbacks callbacks);
    }
}
