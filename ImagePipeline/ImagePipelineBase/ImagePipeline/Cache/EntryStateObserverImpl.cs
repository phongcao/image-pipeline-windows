using System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Provides custom implementation for <see cref="IEntryStateObserver{T}"/>.
    /// </summary>
    public class EntryStateObserverImpl<T> : IEntryStateObserver<T>
    {
        private readonly Action<T, bool> _func;

        /// <summary>
        /// Instantiates the <see cref="EntryStateObserverImpl{T}"/>.
        /// </summary>
        /// <param name="func">Delegate function.</param>
        public EntryStateObserverImpl(Action<T, bool> func)
        {
            _func = func;
        }

        /// <summary>
        /// Invokes the OnExclusivityChanged method.
        /// </summary>
        public void OnExclusivityChanged(T key, bool isExclusive)
        {
            _func(key, isExclusive);
        }
    }
}
