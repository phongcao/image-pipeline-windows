namespace ImagePipelineBase.ImagePipeline.Cache
{
    /// <summary>
    /// Interface used to specify the trimming strategy for the cache.
    /// </summary>
    public interface ICacheTrimStrategy
    {
        /// <summary>
        /// Gets the trim ratio 
        /// </summary>
        /// <param name="trimType"></param>
        /// <returns></returns>
        double GetTrimRatio(double trimType);
    }
}
