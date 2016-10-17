using System;

namespace FBCore.Common.Internal
{
    /// <summary>
    /// Helper class for the <see cref="SupplierHelper&lt;T&gt;"/> interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SupplierHelper<T> : ISupplier<T>
    {
        private readonly Func<T> _func;

        /// <summary>
        /// Instantiates the <see cref="SupplierHelper&lt;T&gt;"/>
        /// </summary>
        public SupplierHelper(Func<T> func)
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
