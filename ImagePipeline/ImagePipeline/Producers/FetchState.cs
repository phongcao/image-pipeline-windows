using ImagePipeline.Image;
using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Used by <see cref="INetworkFetcher{FETCH_STATE}"/> to encapsulate
    /// the state of one network fetch.
    ///
    /// <para />Implementations can subclass this to store additional
    /// fetch-scoped fields.
    /// </summary>
    public class FetchState
    {
        private readonly IConsumer<EncodedImage> _consumer;
        private readonly IProducerContext _context;
        private long _lastIntermediateResultTimeMs;

        /// <summary>
        /// Instantiates the <see cref="FetchState"/>.
        /// </summary>
        public FetchState(
            IConsumer<EncodedImage> consumer,
            IProducerContext context)
        {
            _consumer = consumer;
            _context = context;
            _lastIntermediateResultTimeMs = 0;
        }

        /// <summary>
        /// Gets the consumer.
        /// </summary>
        public IConsumer<EncodedImage> Consumer
        {
            get
            {
                return _consumer;
            }
        }

        /// <summary>
        /// Gets the producer context.
        /// </summary>
        public IProducerContext Context
        {
            get
            {
                return _context;
            }
        }

        /// <summary>
        /// Gets the context id.
        /// </summary>
        public string Id
        {
            get
            {
                return _context.Id;
            }
        }

        /// <summary>
        /// Gets the producer listener.
        /// </summary>
        public IProducerListener Listener
        {
            get
            {
                return _context.Listener;
            }
        }

        /// <summary>
        /// Gets the image request source uri.
        /// </summary>
        public Uri Uri
        {
            get
            {
                return _context.ImageRequest.SourceUri;
            }
        }

        /// <summary>
        /// Gets and sets the last intermediate result time in milliseconds.
        /// </summary>
        public long LastIntermediateResultTimeMs
        {
            get
            {
                return _lastIntermediateResultTimeMs;
            }
            set
            {
                _lastIntermediateResultTimeMs = value;
            }
        }
    }
}
