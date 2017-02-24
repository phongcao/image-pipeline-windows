using System.Threading;

namespace FBCore.Common.Internal
{
    /// <summary>
    /// Provides a bool value that may be updated atomically.
    /// </summary>
    public class AtomicBoolean
    {
        private int _value;

        /// <summary>
        /// Instantiates the <see cref="AtomicBoolean"/>.
        /// </summary>
        /// <param name="value">The initial value.</param>
        public AtomicBoolean(bool value)
        {
            _value = value ? 1 : 0;
        }

        /// <summary>
        /// Returns the current value.
        /// </summary>
        public bool Value
        {
            get
            {
                return Volatile.Read(ref _value) != 0;
            }
            set
            {
                Interlocked.Exchange(ref _value, value ? 1 : 0);
            }
        }
    }
}
