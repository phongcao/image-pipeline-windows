using Cache.Common;
using ImagePipeline.Cache;
using ImagePipeline.Image;
using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Multiplex producer that uses the encoded cache key to combine requests.
    /// </summary>
    public class EncodedCacheKeyMultiplexProducer :
        MultiplexProducer<Tuple<ICacheKey, int>, EncodedImage>
    {
        private readonly ICacheKeyFactory _cacheKeyFactory;

        /// <summary>
        /// Instantiates the <see cref="EncodedCacheKeyMultiplexProducer"/>
        /// </summary>
        public EncodedCacheKeyMultiplexProducer(
            ICacheKeyFactory cacheKeyFactory,
            IProducer<EncodedImage> inputProducer) : 
            base(inputProducer)
        {
            _cacheKeyFactory = cacheKeyFactory;
        }

        /// <summary>
        /// Gets the cache key.
        /// </summary>
        protected override Tuple<ICacheKey, int> GetKey(IProducerContext producerContext)
        {
            return new Tuple<ICacheKey, int>(
                _cacheKeyFactory.GetEncodedCacheKey(
                    producerContext.ImageRequest,
                    producerContext.CallerContext),
                producerContext.LowestPermittedRequestLevel);
        }

        /// <summary>
        /// Clones the result.
        /// </summary>
        public override EncodedImage CloneOrNull(EncodedImage encodedImage)
        {
            return EncodedImage.CloneOrNull(encodedImage);
        }
    }
}
