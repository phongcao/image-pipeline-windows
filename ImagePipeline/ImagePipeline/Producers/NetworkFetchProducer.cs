using FBCore.Common.References;
using FBCore.Common.Time;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// A producer to actually fetch images from the network.
    ///
    /// <para /> Downloaded bytes may be passed to the consumer as they are downloaded, 
    /// but not more often than <see cref="TIME_BETWEEN_PARTIAL_RESULTS_MS"/>.
    /// <para />
    /// Clients should provide an instance of <see cref="INetworkFetcher{FetchState}"/> 
    /// to make use of their networking stack. Use <see cref="HttpUrlConnectionNetworkFetcher"/> 
    /// as a model.
    /// </summary>
    public class NetworkFetchProducer : IProducer<EncodedImage>
    {
        /// <summary>
        /// Producer name
        /// </summary>
        internal const string PRODUCER_NAME = "NetworkFetchProducer";

        /// <summary>
        /// Intermediate result producer event
        /// </summary>
        public const string INTERMEDIATE_RESULT_PRODUCER_EVENT = "intermediate_result";

        /// <summary>
        /// Read size
        /// </summary>
        private const int READ_SIZE = 16 * 1024;

        /// <summary>
        /// Time between two consecutive partial results are propagated upstream
        ///
        /// TODO 5399646: make this configurable
        /// </summary>
        internal const long TIME_BETWEEN_PARTIAL_RESULTS_MS = 100;

        private readonly IPooledByteBufferFactory _pooledByteBufferFactory;
        private readonly IByteArrayPool _byteArrayPool;
        private readonly INetworkFetcher<FetchState> _networkFetcher;

        /// <summary>
        /// Instantiates the <see cref="NetworkFetchProducer"/>
        /// </summary>
        /// <param name="pooledByteBufferFactory"></param>
        /// <param name="byteArrayPool"></param>
        /// <param name="networkFetcher"></param>
        public NetworkFetchProducer(
            IPooledByteBufferFactory pooledByteBufferFactory,
            IByteArrayPool byteArrayPool,
            INetworkFetcher<FetchState> networkFetcher)
        {
            _pooledByteBufferFactory = pooledByteBufferFactory;
            _byteArrayPool = byteArrayPool;
            _networkFetcher = networkFetcher;
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(IConsumer<EncodedImage> consumer, IProducerContext context)
        {
            context.Listener.OnProducerStart(context.Id, PRODUCER_NAME);
            FetchState fetchState = _networkFetcher.CreateFetchState(consumer, context);
            _networkFetcher.Fetch(fetchState, new NetworkFetcherCallbackImpl(
                (response, responseLength) =>
                {
                    OnResponse(fetchState, response, responseLength);
                },
                (throwable) =>
                {
                    OnFailure(fetchState, throwable);
                },
                () =>
                {
                    OnCancellation(fetchState);
                }));
        }

        private void OnResponse(
            FetchState fetchState,
            Stream responseData,
            int responseContentLength)
        {
            PooledByteBufferOutputStream pooledOutputStream;
            if (responseContentLength > 0)
            {
                pooledOutputStream = _pooledByteBufferFactory.NewOutputStream(responseContentLength);
            }
            else
            {
                pooledOutputStream = _pooledByteBufferFactory.NewOutputStream();
            }

            byte[] ioArray = _byteArrayPool.Get(READ_SIZE);
            try
            {
                int length;
                while ((length = responseData.Read(ioArray, 0, ioArray.Length)) >= 0)
                {
                    if (length > 0)
                    {
                        pooledOutputStream.Write(ioArray, 0, length);
                        MaybeHandleIntermediateResult(pooledOutputStream, fetchState);
                        float progress = CalculateProgress(pooledOutputStream.Size, responseContentLength);
                        fetchState.Consumer.OnProgressUpdate(progress);
                    }
                }

                _networkFetcher.OnFetchCompletion(fetchState, pooledOutputStream.Size);
                HandleFinalResult(pooledOutputStream, fetchState);
            }
            finally
            {
                _byteArrayPool.Release(ioArray);
                pooledOutputStream.Dispose();
            }
        }

        private static float CalculateProgress(int downloaded, int total)
        {
            if (total > 0)
            {
                return (float)downloaded / total;
            }
            else
            {
                // If we don't know the total number of bytes, we approximate the progress by an exponential
                // that approaches 1. Here are some values of the progress, given the number of bytes:
                // 0.5 kB ~  1%
                // 2.5 kB ~  5%
                //   5 kB ~ 10%
                //  14 kB ~ 25%
                //  34 kB ~ 50%
                //  68 kB ~ 75%
                // 113 kB ~ 90%
                // 147 kB ~ 95%
                // 225 kB ~ 99%
                return 1 - (float)Math.Exp(-downloaded / 5e4);
            }
        }

        private void MaybeHandleIntermediateResult(
            PooledByteBufferOutputStream pooledOutputStream,
            FetchState fetchState)
        {
            long nowMs = SystemClock.UptimeMillis;
            if (ShouldPropagateIntermediateResults(fetchState) &&
                nowMs - fetchState.LastIntermediateResultTimeMs >= TIME_BETWEEN_PARTIAL_RESULTS_MS)
            {
                fetchState.LastIntermediateResultTimeMs = nowMs;
                fetchState.Listener.OnProducerEvent(
                    fetchState.Id, PRODUCER_NAME, INTERMEDIATE_RESULT_PRODUCER_EVENT);
                NotifyConsumer(pooledOutputStream, false, fetchState.Consumer);
            }
        }

        private void HandleFinalResult(
            PooledByteBufferOutputStream pooledOutputStream,
            FetchState fetchState)
        {
            IDictionary<string, string> extraMap = GetExtraMap(fetchState, pooledOutputStream.Size);
            fetchState.Listener.OnProducerFinishWithSuccess(fetchState.Id, PRODUCER_NAME, extraMap);
            NotifyConsumer(pooledOutputStream, true, fetchState.Consumer);
        }

        private void NotifyConsumer(
            PooledByteBufferOutputStream pooledOutputStream,
            bool isFinal,
            IConsumer<EncodedImage> consumer)
        {
            CloseableReference<IPooledByteBuffer> result = 
                CloseableReference<IPooledByteBuffer>.of(pooledOutputStream.ToByteBuffer());
            EncodedImage encodedImage = null;
            try
            {
                encodedImage = new EncodedImage(result);
                encodedImage.ParseMetaDataAsync().RunSynchronously();
                consumer.OnNewResult(encodedImage, isFinal);
            }
            finally
            {
                EncodedImage.CloseSafely(encodedImage);
                CloseableReference<IPooledByteBuffer>.CloseSafely(result);
            }
        }

        private void OnFailure(FetchState fetchState, Exception e)
        {
            fetchState.Listener.OnProducerFinishWithFailure(fetchState.Id, PRODUCER_NAME, e, null);
            fetchState.Consumer.OnFailure(e);
        }

        private void OnCancellation(FetchState fetchState)
        {
            fetchState.Listener.OnProducerFinishWithCancellation(fetchState.Id, PRODUCER_NAME, null);
            fetchState.Consumer.OnCancellation();
        }

        private bool ShouldPropagateIntermediateResults(FetchState fetchState)
        {
            if (!fetchState.Context.ImageRequest.IsProgressiveRenderingEnabled)
            {
                return false;
            }

            return _networkFetcher.ShouldPropagate(fetchState);
        }

        private IDictionary<string, string> GetExtraMap(FetchState fetchState, int byteSize)
        {
            if (!fetchState.Listener.RequiresExtraMap(fetchState.Id))
            {
                return null;
            }

            return _networkFetcher.GetExtraMap(fetchState, byteSize);
        }
    }
}
