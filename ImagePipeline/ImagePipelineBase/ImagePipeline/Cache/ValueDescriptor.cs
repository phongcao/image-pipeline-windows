using System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// ValueDescriptor helper class
    /// </summary>
    public class ValueDescriptor<T> : IValueDescriptor<T>
    {
        private readonly Func<T, int> _func;

        /// <summary>
        /// Instantiates the <see cref="ValueDescriptor&lt;T&gt;"/>.
        /// </summary>
        /// <param name="func">Delegate function</param>
        public ValueDescriptor(Func<T, int> func)
        {
            _func = func;
        }

        /// <summary>
        /// Invokes the GetSizeInBytes method
        /// </summary>
        /// <param name="value">T</param>
        public int GetSizeInBytes(T value)
        {
            return _func(value);
        }
    }
}
