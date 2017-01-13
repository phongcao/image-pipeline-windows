using FBCore.Common.References;
using FBCore.DataSource;
using ImagePipeline.Listener;
using ImagePipeline.Producers;

namespace ImagePipeline.Datasource
{
    /// <summary>
    /// IDataSource{CloseableReference{T}} backed by a Producer{CloseableReference{T}}
    /// </summary>
    public class CloseableProducerToDataSourceAdapter<T> : AbstractProducerToDataSourceAdapter<CloseableReference<T>>
    {
        /// <summary>
        /// CloseableProducerToDataSourceAdapter factory method
        /// </summary>
        public static IDataSource<CloseableReference<T>> Create(
            IProducer<CloseableReference<T>> producer,
            SettableProducerContext settableProducerContext,
            IRequestListener listener)
        {
            return new CloseableProducerToDataSourceAdapter<T>(
                producer, settableProducerContext, listener);
        }

        private CloseableProducerToDataSourceAdapter(
            IProducer<CloseableReference<T>> producer,
            SettableProducerContext settableProducerContext,
            IRequestListener listener) : base(
                producer, 
                settableProducerContext, 
                listener)
        {
        }

        /// <summary>
        /// The most recent result of the asynchronous computation.
        /// </summary>
        /// <returns></returns>
        public override CloseableReference<T> GetResult()
        {
            return CloseableReference<T>.CloneOrNull(base.GetResult());
        }

        /// <summary>
        /// This method is called in two cases:
        /// 1. to clear the result when data source gets closed
        /// 2. to clear the previous result when a new result is set
        /// </summary>
        public override void CloseResult(CloseableReference<T> result)
        {
            CloseableReference<T>.CloseSafely(result);
        }

        /// <summary>
        /// OnNewResult implementation
        /// </summary>
        /// <param name="result"></param>
        /// <param name="isLast"></param>
        protected override void OnNewResultImpl(CloseableReference<T> result, bool isLast)
        {
            base.OnNewResultImpl(CloseableReference<T>.CloneOrNull(result), isLast);
        }
    }
}
