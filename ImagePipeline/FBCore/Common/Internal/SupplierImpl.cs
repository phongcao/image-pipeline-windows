using System;
using System.Threading;

namespace FBCore.Common.Internal
{
    /// <summary>
    /// Provides custom implementation for <see cref="ISupplier{T}"/>
    /// </summary>
    public class SupplierImpl<T> : ISupplier<T>
    {
        private readonly Func<T> _getFunc;
        private readonly Func<string> _toStringFunc;

        /// <summary>
        /// Test-only variables
        ///
        /// <para /><b>DO NOT USE in application code.</b>
        /// </summary>
        private int _getCallCount;

        /// <summary>
        /// Instantiates the <see cref="SupplierImpl{T}"/>
        /// </summary>
        public SupplierImpl(Func<T> getFunc, Func<string> toStringFunc)
        {
            _getFunc = getFunc;
            _toStringFunc = toStringFunc;

            // For unit test
            _getCallCount = 0;
        }

        /// <summary>
        /// Instantiates the <see cref="SupplierImpl{T}"/>
        /// </summary>
        public SupplierImpl(Func<T> getFunc) : this(getFunc, null)
        {
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

            return _getFunc();
        }

        /// <summary>
        /// Overrides the base ToString method.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_toStringFunc != null)
            {
                return _toStringFunc();
            }

            return base.ToString();
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
