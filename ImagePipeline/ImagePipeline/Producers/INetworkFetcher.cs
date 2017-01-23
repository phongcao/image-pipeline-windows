using FBCore.Concurrency;
using ImagePipeline.Image;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Interface that specifies network fetcher used by the image pipeline.
    ///
    /// <para />It is strongly recommended that implementations use an 
    /// <see cref="IExecutorService"/> in their Fetch method to execute the 
    /// network request on a different thread.
    ///
    /// <para /> When the fetch from the network fails or is cancelled, the subclass is responsible for
    /// calling Callback methods. If these are not called, the pipeline will not know that the
    /// image fetch has failed and the application may not behave proper
    /// </summary>
    public interface INetworkFetcher<FETCH_STATE> where FETCH_STATE : FetchState
    {
        /// <summary>
        /// Creates a new instance of the <see cref="FetchState"/>-derived object used to store state.
        ///
        /// <param name="consumer">the consumer</param>
        /// <param name="producerContext">the producer's context</param>
        /// @return a new fetch state instance
        /// </summary>
        FETCH_STATE CreateFetchState(
            IConsumer<EncodedImage> consumer,
            IProducerContext producerContext);

        /// <summary>
        /// Initiates the network fetch and informs the producer when a response is received via the
        /// provided callback.
        ///
        /// <param name="fetchState">the fetch-specific state</param>
        /// <param name="callback">the callback used to inform the network fetch producer</param>
        /// </summary>
        void Fetch(FETCH_STATE fetchState, INetworkFetcherCallback callback);

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
        bool ShouldPropagate(FETCH_STATE fetchState);

        /// <summary>
        /// Called after the fetch completes.
        ///
        /// <para /> Implementing this method is optional and is useful for instrumentation purposes.
        ///
        /// <param name="fetchState">the fetch-specific state</param>
        /// <param name="byteSize">size of the data in bytes</param>
        /// </summary>
        void OnFetchCompletion(FETCH_STATE fetchState, int byteSize);

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
        IDictionary<string, string> GetExtraMap(FETCH_STATE fetchState, int byteSize);
    }

    /// <summary>
    /// Callback used to inform the network fetch producer.
    /// </summary>
    public interface INetworkFetcherCallback
    {
        /// <summary>
        /// Called upon a response from the network stack.
        ///
        /// <param name="response">the stream for the data</param>
        /// <param name="responseLength">the length of the data if known, -1 otherwise</param>
        /// </summary>
        void OnResponse(Stream response, int responseLength);

        /// <summary>
        /// Called upon a failure in the network stack.
        ///
        /// <param name="throwable">the cause of failure</param>
        /// </summary>
        void OnFailure(Exception throwable);

        /// <summary>
        /// Called upon a cancellation of the request.
        /// </summary>
        void OnCancellation();
    }

    /// <summary>
    /// Provides the custom implementation for <see cref="INetworkFetcherCallback"/>
    /// </summary>
    public class NetworkFetcherCallbackImpl : INetworkFetcherCallback
    {
        private Action<Stream, int> _onResponseFunc;
        private Action<Exception> _onFailureFunc;
        private Action _onCancellationFunc;

        /// <summary>
        /// Instantiates the <see cref="NetworkFetcherCallbackImpl"/>
        /// </summary>
        public NetworkFetcherCallbackImpl(
            Action<Stream, int> onResponseFunc,
            Action<Exception> onFailureFunc,
            Action onCancellationFunc)
        {
            _onResponseFunc = onResponseFunc;
            _onFailureFunc = onFailureFunc;
            _onCancellationFunc = onCancellationFunc;
        }

        /// <summary>
        /// Called upon a response from the network stack.
        ///
        /// <param name="response">the stream for the data</param>
        /// <param name="responseLength">the length of the data if known, -1 otherwise</param>
        /// </summary>
        public void OnResponse(Stream response, int responseLength)
        {
            _onResponseFunc(response, responseLength);
        }

        /// <summary>
        /// Called upon a failure in the network stack.
        ///
        /// <param name="throwable">the cause of failure</param>
        /// </summary>
        public void OnFailure(Exception throwable)
        {
            _onFailureFunc(throwable);
        }

        /// <summary>
        /// Called upon a cancellation of the request.
        /// </summary>
        public void OnCancellation()
        {
            _onCancellationFunc();
        }
    }
}
