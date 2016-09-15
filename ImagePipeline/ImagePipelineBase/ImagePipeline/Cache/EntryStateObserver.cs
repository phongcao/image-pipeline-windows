using System;

namespace ImagePipelineBase.ImagePipeline.Cache
{
    /// <summary>
    /// EntryStateObserver helper class
    /// </summary>
    public class EntryStateObserver<T> : IEntryStateObserver<T>
    {
        private readonly Action<T, bool> _func;

        /// <summary>
        /// Instantiates the <see cref="EntryStateObserver&lt;T&gt;"/>.
        /// </summary>
        /// <param name="func">Delegate function</param>
        public EntryStateObserver(Action<T, bool> func)
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
