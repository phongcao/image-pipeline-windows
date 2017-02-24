namespace ImagePipeline.Request
{
    /// <summary>
    /// Base RepeatedPostProcessor.
    /// </summary>
    public abstract class BaseRepeatedPostprocessor : BasePostprocessor, IRepeatedPostprocessor
    {
        private readonly object _gate = new object();
        private IRepeatedPostprocessorRunner _callback;

        /// <summary>
        /// Callback used to pass the postprocessor a reference to the object
        /// that will run the postprocessor's <code>IPostProcessor.Process</code>
        /// method when the client requires.
        /// </summary>
        public void SetCallback(IRepeatedPostprocessorRunner runner)
        {
            lock (_gate)
            {
                _callback = runner;
            }
        }

        private IRepeatedPostprocessorRunner GetCallback()
        {
            lock (_gate)
            {
                return _callback;
            }
        }

        /// <summary>
        /// Used when a new postprocessing of the last received result is requested.
        /// </summary>
        public void Update()
        {
            IRepeatedPostprocessorRunner callback = GetCallback();
            if (callback != null)
            {
                callback.Update();
            }
        }
    }
}
