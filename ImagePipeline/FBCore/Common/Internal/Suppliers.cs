namespace FBCore.Common.Internal
{
    /// <summary>
    /// Wrapper for creating a ISupplier.
    /// </summary>
    public class Suppliers
    {
        /// <summary>
        /// Returns a ISupplier which always returns <code>instance</code>.
        /// </summary>
        /// <param name="instance">The instance that should always be provided.</param>
        public static ISupplier<T> of<T>(T instance)
        {
            return new SupplierImpl<T>(() =>
            {
                return instance;
            });
        }
    }
}
