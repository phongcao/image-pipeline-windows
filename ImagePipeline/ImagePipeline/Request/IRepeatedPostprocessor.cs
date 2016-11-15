namespace ImagePipeline.Request
{
    /// <summary>
    /// Use an instance of this interface to perform post-process operations that must be performed
    /// more than once.
    /// </summary>
    public interface IRepeatedPostprocessor
    {
        /// <summary>
        /// Callback used to pass the postprocessor a reference to the object that will run the
        /// postprocessor's <code> PostProcessor#process</code> method when the client requires.
        /// <param name="runner"></param>
        /// </summary>
        void SetCallback(IRepeatedPostprocessorRunner runner);
    }
}
