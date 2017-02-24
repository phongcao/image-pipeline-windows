using System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Provides custom implementation for <see cref="IValueDescriptor{T}"/>.
    /// </summary>
    public class ValueDescriptorImpl<T> : IValueDescriptor<T>
    {
        private readonly Func<T, int> _func;

        /// <summary>
        /// Instantiates the <see cref="ValueDescriptorImpl{T}"/>.
        /// </summary>
        /// <param name="func">Delegate function.</param>
        public ValueDescriptorImpl(Func<T, int> func)
        {
            _func = func;
        }

        /// <summary>
        /// Invokes the GetSizeInBytes method.
        /// </summary>
        /// <param name="value">T.</param>
        public int GetSizeInBytes(T value)
        {
            return _func(value);
        }
    }
}
