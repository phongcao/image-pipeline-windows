using ImagePipeline.Image;
using System.Collections.Generic;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Base class for <see cref="INetworkFetcher{FETCH_STATE}"/>.
    ///
    /// <para /> Intermediate results are propagated.
    /// <para /> GetExtraMap returns null.
    /// </summary>
    public abstract class BaseNetworkFetcher<FETCH_STATE> : INetworkFetcher<FETCH_STATE> 
        where FETCH_STATE : FetchState
    {
        /// <summary>
        /// Gets whether the intermediate results should be propagated.
        ///
        /// <para />In <i>addition</i> to the requirements of this method, intermediate results are throttled so
        /// that a maximum of one every 100 ms is propagated. This is to conserve CPU and other resources.
        ///
        /// <para />Not applicable if progressive rendering is disabled or not supported for this image.
        ///
        /// <param name="fetchState">the fetch-specific state</param>
        /// @return whether the intermediate results should be propagated
        /// </summary>
        public bool ShouldPropagate(FETCH_STATE fetchState)
        {
            return true;
        }

        /// <summary>
        /// Called after the fetch completes.
        ///
        /// <para /> Implementing this method is optional and is useful for instrumentation purposes.
        ///
        /// <param name="fetchState">the fetch-specific state</param>
        /// <param name="byteSize">size of the data in bytes</param>
        /// </summary>
        public void OnFetchCompletion(FETCH_STATE fetchState, int byteSize)
        {
            // no-op
        }

        /// <summary>
        /// Gets a map containing extra parameters to pass to the listeners.
        ///
        /// <para /> Returning map is optional and is useful for instrumentation purposes.
        ///
        /// <para /> This map won't be modified by the caller.
        ///
        /// <param name="fetchState">the fetch-specific state</param>
        /// <param name="byteSize">size of the data in bytes</param>
        /// @return a map with extra parameters
        /// </summary>
        public IDictionary<string, string> GetExtraMap(FETCH_STATE fetchState, int byteSize)
        {
            return null;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FetchState"/>-derived object used to store state.
        ///
        /// <param name="consumer">the consumer</param>
        /// <param name="producerContext">the producer's context</param>
        /// @return a new fetch state instance
        /// </summary>
        public abstract FETCH_STATE CreateFetchState(
            IConsumer<EncodedImage> consumer,
            IProducerContext producerContext);

        /// <summary>
        /// Initiates the network fetch and informs the producer when a response is received via the
        /// provided callback.
        ///
        /// <param name="fetchState">the fetch-specific state</param>
        /// <param name="callback">the callback used to inform the network fetch producer</param>
        /// </summary>
        public abstract void Fetch(FETCH_STATE fetchState, INetworkFetcherCallback callback);
    }
}
