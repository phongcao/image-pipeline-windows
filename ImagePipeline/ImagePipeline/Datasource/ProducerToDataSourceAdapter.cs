using FBCore.DataSource;
using ImagePipeline.Listener;
using ImagePipeline.Producers;

namespace ImagePipeline.Datasource
{
    /// <summary>
    /// IDataSource{T} backed by a IProducer{T}.
    /// </summary>
    public class ProducerToDataSourceAdapter<T> : AbstractProducerToDataSourceAdapter<T>
    {
        /// <summary>
        /// ProducerToDataSourceAdapter factory method.
        /// </summary>
        public static IDataSource<T> Create(
            IProducer<T> producer,
            SettableProducerContext settableProducerContext,
            IRequestListener listener)
        {
            return new ProducerToDataSourceAdapter<T>(
                producer,
                settableProducerContext,
                listener);
        }

        private ProducerToDataSourceAdapter(
            IProducer<T> producer,
            SettableProducerContext settableProducerContext,
            IRequestListener listener) : base(
                producer,
                settableProducerContext,
                listener)
        {
        }
    }
}
