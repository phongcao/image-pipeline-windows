namespace ImagePipeline.Request
{
    /// <summary>
    /// An instance of this class is used to run a postprocessor whenever
    /// the client requires.
    /// </summary>
    public interface IRepeatedPostprocessorRunner
    {
        /// <summary>
        /// Used when a new postprocessing of the last received result
        /// is requested.
        /// </summary>
        void Update();
    }
}
