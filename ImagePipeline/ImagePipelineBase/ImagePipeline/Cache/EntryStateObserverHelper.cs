using System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// EntryStateObserver helper class
    /// </summary>
    public class EntryStateObserverHelper<T> : IEntryStateObserver<T>
    {
        private readonly Action<T, bool> _func;

        /// <summary>
        /// Instantiates the <see cref="EntryStateObserverHelper{T}"/>.
        /// </summary>
        /// <param name="func">Delegate function</param>
        public EntryStateObserverHelper(Action<T, bool> func)
        {
            _func = func;
        }

        /// <summary>
        /// Invokes the OnExclusivityChanged method
        /// </summary>
        public void OnExclusivityChanged(T key, bool isExclusive)
        {
            _func(key, isExclusive);
        }
    }
}
