using System;
using System.Threading;

namespace FBCore.Common.Internal
{
    /// <summary>
    /// Provides custom implementation for <see cref="ISupplier{T}"/>
    /// </summary>
    public class SupplierImpl<T> : ISupplier<T>
    {
        private readonly Func<T> _func;

        /// <summary>
        /// Test-only variables
        ///
        /// <para /><b>DO NOT USE in application code.</b>
        /// </summary>
        private int _getCallCount;

        /// <summary>
        /// Instantiates the <see cref="SupplierImpl{T}"/>
        /// </summary>
        public SupplierImpl(Func<T> func)
        {
            _func = func;

            // For unit test
            _getCallCount = 0;
        }

        /// <summary>
        /// Retrieves an instance of the appropriate type. The returned object may or
        /// may not be a new instance, depending on the implementation.
        ///
        /// @return an instance of the appropriate type
        /// </summary>
        public T Get()
        {
            // For unit test
            Interlocked.Increment(ref _getCallCount);

            return _func();
        }

        /// <summary>
        /// For unit test
        /// </summary>
        internal int GetCallCount
        {
            get
            {
                return Volatile.Read(ref _getCallCount);
            }
        }
    }
}
