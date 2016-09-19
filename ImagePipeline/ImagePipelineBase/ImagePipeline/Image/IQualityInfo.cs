namespace ImagePipeline.Image
{
    /// <summary>
    /// Interface for image quality information
    /// </summary>
    public interface IQualityInfo
    {
        /// <summary>
        /// Used only to compare quality of two images that points to the same resource (uri).
        /// <para /> Higher number means higher quality.
        /// <para /> This is useful for caching in order to determine whether the new result is of higher
        /// quality than what's already in the cache.
        /// </summary>
        int Quality { get; }

        /// <summary>
        /// Whether the image is of good-enough quality.
        /// <para /> When fetching image progressively, the few first results can be of really poor quality,
        /// but eventually, they get really close to original image, and we mark those as good-enough.
        /// </summary>
        bool IsOfGoodEnoughQuality { get; }

        /// <summary>
        /// Whether the image is of full quality.
        /// <para /> For progressive JPEGs, this is the final scan. For other image types, this is always true.
        /// </summary>
        bool IsOfFullQuality { get; }
    }
}
