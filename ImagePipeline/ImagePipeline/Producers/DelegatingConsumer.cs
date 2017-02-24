using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Delegating consumer.
    /// </summary>
    public abstract class DelegatingConsumer<I, O> : BaseConsumer<I>
    {
        private readonly IConsumer<O> _consumer;

        /// <summary>
        /// Instantiates the <see cref="DelegatingConsumer{I, O}"/>.
        /// </summary>
        public DelegatingConsumer(IConsumer<O> consumer)
        {
            _consumer = consumer;
        }

        /// <summary>
        /// Gets the consumer.
        /// </summary>
        public IConsumer<O> Consumer
        {
            get
            {
                return _consumer;
            }
        }

        /// <summary>
        /// Called by OnFailure, override this method instead.
        /// </summary>
        protected override void OnFailureImpl(Exception error)
        {
            _consumer.OnFailure(error);
        }

        /// <summary>
        /// Called by OnCancellation, override this method instead.
        /// </summary>
        protected override void OnCancellationImpl()
        {
            _consumer.OnCancellation();
        }

        /// <summary>
        /// Called when the progress updates.
        /// </summary>
        protected override void OnProgressUpdateImpl(float progress)
        {
            _consumer.OnProgressUpdate(progress);
        }
    }
}
