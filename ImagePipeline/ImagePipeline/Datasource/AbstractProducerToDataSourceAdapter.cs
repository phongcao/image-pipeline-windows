using FBCore.Common.Internal;
using FBCore.DataSource;
using ImagePipeline.Listener;
using ImagePipeline.Producers;
using System;

namespace ImagePipeline.Datasource
{
    /// <summary>
    /// DataSource{T} backed by a IProducer{T}
    /// </summary>
    public abstract class AbstractProducerToDataSourceAdapter<T> : AbstractDataSource<T>
    {
        private readonly object _gate = new object();

        private readonly SettableProducerContext _settableProducerContext;
        private readonly IRequestListener _requestListener;

        /// <summary>
        /// Instantiates the <see cref="AbstractProducerToDataSourceAdapter{T}"/>
        /// </summary>
        /// <param name="producer"></param>
        /// <param name="settableProducerContext"></param>
        /// <param name="requestListener"></param>
        protected AbstractProducerToDataSourceAdapter(
            IProducer<T> producer,
            SettableProducerContext settableProducerContext,
            IRequestListener requestListener)
        {
            _settableProducerContext = settableProducerContext;
            _requestListener = requestListener;
            _requestListener.OnRequestStart(
                _settableProducerContext.ImageRequest,
                _settableProducerContext.CallerContext,
                _settableProducerContext.Id,
                _settableProducerContext.IsPrefetch);
            producer.ProduceResults(CreateConsumer(), settableProducerContext);
        }

        private IConsumer<T> CreateConsumer()
        {
            return new BaseConsumerImpl<T>(
                (newResult, isLast) =>
                {
                    OnNewResultImpl(newResult, isLast);
                },
                (throwable) =>
                {
                    OnFailureImpl(throwable);
                },
                () =>
                {
                    OnCancellationImpl();
                },
                (progress) =>
                {
                    SetProgress(progress);
                }
            );
        }

        /// <summary>
        /// OnNewResult implementation
        /// </summary>
        /// <param name="result"></param>
        /// <param name="isLast"></param>
        protected virtual void OnNewResultImpl(T result, bool isLast)
        {
            if (SetResult(result, isLast))
            {
                if (isLast)
                {
                    _requestListener.OnRequestSuccess(
                        _settableProducerContext.ImageRequest,
                        _settableProducerContext.Id,
                        _settableProducerContext.IsPrefetch);
                }
            }
        }

        /// <summary>
        /// OnFailure implementation
        /// </summary>
        /// <param name="throwable"></param>
        private void OnFailureImpl(Exception throwable)
        {
            if (SetFailure(throwable))
            {
                _requestListener.OnRequestFailure(
                    _settableProducerContext.ImageRequest,
                    _settableProducerContext.Id,
                    throwable,
                    _settableProducerContext.IsPrefetch);
            }
        }

        private void OnCancellationImpl()
        {
            lock (_gate)
            {
                Preconditions.CheckState(IsClosed());
            }
        }

        /// <summary>
        /// Cancels the ongoing request and releases all associated resources.
        /// </summary>

        public override bool Close()
        {
            if (!base.Close())
            {
                return false;
            }

            if (!IsFinished())
            {
                _requestListener.OnRequestCancellation(_settableProducerContext.Id);
                _settableProducerContext.Cancel();
            }

            return true;
        }
    }
}
