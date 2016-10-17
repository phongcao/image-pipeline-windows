namespace FBCore.Common.Internal
{
    /// <summary>
    /// Wrapper for creating a Supplier.
    /// </summary>
    public class Suppliers
    {
        /// <summary>
        /// Returns a Supplier which always returns <code> instance</code>.
        ///
        /// <param name="instance">the instance that should always be provided.</param>
        /// </summary>
        public static ISupplier<T> of<T>(T instance)
        {
            return new SupplierHelper<T>(() =>
            {
                return instance;
            });
        }
    }
}
