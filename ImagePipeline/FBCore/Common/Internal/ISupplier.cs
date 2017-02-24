namespace FBCore.Common.Internal
{
    /// <summary>
    /// A class that can supply objects of a single type. Semantically, this could
    /// be a factory, generator, builder, closure, or something else entirely. 
    /// No guarantees are implied by this interface.
    ///
    /// @author Harry Heymann
    /// @since 2.0 (imported from Google Collections Library)
    /// </summary>
    public interface ISupplier<T>
    {
        /// <summary>
        /// Retrieves an instance of the appropriate type. The returned object may or
        /// may not be a new instance, depending on the implementation.
        /// </summary>
        /// <returns>An instance of the appropriate type.</returns>
        T Get();
    }
}
