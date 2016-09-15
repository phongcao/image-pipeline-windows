namespace ImagePipelineBase.ImagePipeline.Cache
{
    /// <summary>
    /// Interface used to observe the state changes of an entry.
    /// </summary>
    public interface IEntryStateObserver<T>
    {
        /// <summary>
        /// Called when the exclusivity status of the entry changes.
        ///
        /// <para /> The item can be reused if it is exclusively owned by the cache.
        /// </summary>
        void OnExclusivityChanged(T key, bool isExclusive);
    }
}
