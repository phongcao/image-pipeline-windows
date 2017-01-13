namespace ImagePipeline.Producers
{
    /// <summary>
    /// Callbacks that are called when something changes in a request sequence.
    /// </summary>
    public interface IProducerContextCallbacks
    {
        /// <summary>
        /// Method that is called when a client cancels the request.
        /// </summary>
        void OnCancellationRequested();

        /// <summary>
        /// Method that is called when a request is no longer a prefetch, or vice versa.
        /// </summary>
        void OnIsPrefetchChanged();

        /// <summary>
        /// Method that is called when intermediate results start or stop being expected.
        /// </summary>
        void OnIsIntermediateResultExpectedChanged();

        /// <summary>
        /// Method that is called when the priority of the request changes.
        /// </summary>
        void OnPriorityChanged();
    }
}
