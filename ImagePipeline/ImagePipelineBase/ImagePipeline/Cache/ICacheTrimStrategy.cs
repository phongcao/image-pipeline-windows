namespace ImagePipeline.Cache
{
    /// <summary>
    /// Interface used to specify the trimming strategy for the cache.
    /// </summary>
    public interface ICacheTrimStrategy
    {
        /// <summary>
        /// Gets the trim ratio.
        /// </summary>
        double GetTrimRatio(double trimType);
    }
}
