namespace FBCore.Common.References
{
    /// <summary>
    /// Interface that abstracts the action of releasing a resource.
    ///
    /// <para />There are multiple components that own resources that are shared by others,
    /// like pools and caches. This interface should be implemented by classes that want to
    /// perform some action when a particular resource is no longer needed.
    /// {T} type of resource managed by this ResourceReleaser.
    /// </summary>
    public interface IResourceReleaser<T>
    {
        /// <summary>
        /// <para />Release the given value.
        ///
        /// <para />After calling this method, the caller is no longer responsible for
        /// managing lifetime of the value.
        /// <para />This method is not permitted to throw an exception and is always
        /// required to succeed. It is often called from contexts like catch blocks or
        /// finally blocks to cleanup resources.
        /// Throwing an exception could result in swallowing the original exception.
        /// </summary>
        /// <param name="value">T</param>
        void Release(T value);
    }
}
