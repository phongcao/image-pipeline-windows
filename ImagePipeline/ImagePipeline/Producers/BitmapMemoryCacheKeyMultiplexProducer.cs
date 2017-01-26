using Cache.Common;
using FBCore.Common.References;
using ImagePipeline.Cache;
using ImagePipeline.Image;
using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Multiplex producer that uses the bitmap memory cache key to combine requests.
    /// </summary>
    public class BitmapMemoryCacheKeyMultiplexProducer :
        MultiplexProducer<Tuple<ICacheKey, int>, CloseableReference<CloseableImage>>
    {
        private readonly ICacheKeyFactory _cacheKeyFactory;

        /// <summary>
        /// Instantiates the <see cref="BitmapMemoryCacheKeyMultiplexProducer"/>
        /// </summary>
        public BitmapMemoryCacheKeyMultiplexProducer(
            ICacheKeyFactory cacheKeyFactory,
            IProducer<CloseableReference<CloseableImage>> inputProducer) :
            base(inputProducer)
        {
            _cacheKeyFactory = cacheKeyFactory;
        }

        /// <summary>
        /// Gets the cache key.
        /// </summary>
        protected override Tuple<ICacheKey, int> GetKey(
            IProducerContext producerContext)
        {
            return new Tuple<ICacheKey, int>(
                _cacheKeyFactory.GetBitmapCacheKey(
                    producerContext.ImageRequest,
                    producerContext.CallerContext),
                    producerContext.LowestPermittedRequestLevel);
        }

        /// <summary>
        /// Clones the result.
        /// </summary>
        public override CloseableReference<CloseableImage> CloneOrNull(
            CloseableReference<CloseableImage> closeableImage)
        {
            return CloseableReference<CloseableImage>.CloneOrNull(closeableImage);
        }
    }
}
