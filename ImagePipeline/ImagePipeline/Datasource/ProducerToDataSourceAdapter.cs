using FBCore.DataSource;
using ImagePipeline.Listener;
using ImagePipeline.Producers;

namespace ImagePipeline.Datasource
{
    /// <summary>
    /// DataSource{T} backed by a Producer{T}
    /// </summary>
    public class ProducerToDataSourceAdapter<T> : AbstractProducerToDataSourceAdapter<T>
    {
        /// <summary>
        /// ProducerToDataSourceAdapter factory method
        /// </summary>
        /// <param name="producer"></param>
        /// <param name="settableProducerContext"></param>
        /// <param name="listener"></param>
        /// <returns></returns>
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
