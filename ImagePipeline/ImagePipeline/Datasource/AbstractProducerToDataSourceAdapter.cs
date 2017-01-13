using FBCore.DataSource;

namespace ImagePipeline.Datasource
{
    /// <summary>
    /// DataSource{T} backed by a IProducer{T}
    /// </summary>
    public abstract class AbstractProducerToDataSourceAdapter<T> : AbstractDataSource<T>
    {
        //private readonly SettableProducerContext mSettableProducerContext;
        //private readonly RequestListener mRequestListener;

        //protected AbstractProducerToDataSourceAdapter(
        //    Producer<T> producer,
        //    SettableProducerContext settableProducerContext,
        //    RequestListener requestListener)
        //{
        //    mSettableProducerContext = settableProducerContext;
        //    mRequestListener = requestListener;
        //    mRequestListener.onRequestStart(
        //        settableProducerContext.getImageRequest(),
        //        mSettableProducerContext.getCallerContext(),
        //        mSettableProducerContext.getId(),
        //        mSettableProducerContext.isPrefetch());
        //    producer.produceResults(createConsumer(), settableProducerContext);
        //}
    }
}
