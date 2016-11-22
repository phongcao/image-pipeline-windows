using System;

namespace FBCore.Common.Internal
{
    /// <summary>
    /// Provides custom implementation for <see cref="ISupplier{T}"/>
    /// </summary>
    public class SupplierImpl<T> : ISupplier<T>
    {
        private readonly Func<T> _func;

        /// <summary>
        /// Instantiates the <see cref="SupplierImpl{T}"/>
        /// </summary>
        public SupplierImpl(Func<T> func)
        {
            _func = func;
        }

        /// <summary>
        /// Retrieves an instance of the appropriate type. The returned object may or
        /// may not be a new instance, depending on the implementation.
        ///
        /// @return an instance of the appropriate type
        /// </summary>
        public T Get()
        {
            return _func();
        }
    }
}
