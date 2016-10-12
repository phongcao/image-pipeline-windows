namespace ImagePipeline.Image
{
    /// <summary>
    /// Implementation of <see cref="IQualityInfo"/>
    /// </summary>
    public class ImmutableQualityInfo : IQualityInfo
    {
        /// <summary>
        /// Default full quality value
        /// </summary>
        public static readonly IQualityInfo FULL_QUALITY = of(int.MaxValue, true, true);

        internal int _quality;
        internal bool _isOfGoodEnoughQuality;
        internal bool _isOfFullQuality;

        private ImmutableQualityInfo(
            int quality,
            bool isOfGoodEnoughQuality,
            bool isOfFullQuality)
        {
            _quality = quality;
            _isOfGoodEnoughQuality = isOfGoodEnoughQuality;
            _isOfFullQuality = isOfFullQuality;
        }

        /// <summary>
        /// Used only to compare quality of two images that points to the same resource (uri).
        /// <para /> Higher number means higher quality.
        /// <para /> This is useful for caching in order to determine whether the new result is of higher
        /// quality than what's already in the cache.
        /// </summary>
        public int Quality
        {
            get
            {
                return _quality;
            }
        }

        /// <summary>
        /// Whether the image is of good-enough quality.
        /// <para /> When fetching image progressively, the few first results can be of really poor quality,
        /// but eventually, they get really close to original image, and we mark those as good-enough.
        /// </summary>
        public bool GoodEnoughQuality
        {
            get
            {
                return _isOfGoodEnoughQuality;
            }
        }

        /// <summary>
        /// Whether the image is of full quality.
        /// <para /> For progressive JPEGs, this is the final scan. For other image types, this is always true.
        /// </summary>
        public bool FullQuality
        {
            get
            {
                return _isOfFullQuality;
            }
        }

        /// <summary>
        /// Returns the hash code of the quality
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _quality ^ (_isOfGoodEnoughQuality ? 0x400000 : 0) ^ (_isOfFullQuality ? 0x800000 : 0);
        }

        /// <summary>
        /// Compares with other quality
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }

            if (!(other.GetType() == typeof(ImmutableQualityInfo)))
            {
                return false;
            }

            ImmutableQualityInfo that = (ImmutableQualityInfo)other;
            return _quality == that._quality &&
                _isOfGoodEnoughQuality == that._isOfGoodEnoughQuality &&
                _isOfFullQuality == that._isOfFullQuality;
        }

        /// <summary>
        /// Instantiates the <see cref="ImmutableQualityInfo"/>.
        /// </summary>
        public static IQualityInfo of(
            int quality,
            bool isOfGoodEnoughQuality,
            bool isOfFullQuality)
        {
            return new ImmutableQualityInfo(quality, isOfGoodEnoughQuality, isOfFullQuality);
        }
    }
}
