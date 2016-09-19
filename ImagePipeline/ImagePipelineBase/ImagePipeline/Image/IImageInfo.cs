namespace ImagePipeline.Image
{
    /// <summary>
    /// Interface containing information about an image.
    /// </summary>
    public interface IImageInfo
    {
        /// <summary>
        /// Returns width of the image
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Returns height of the image
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Returns quality information for the image
        /// </summary>
        IQualityInfo QualityInfo { get; }
    }
}
