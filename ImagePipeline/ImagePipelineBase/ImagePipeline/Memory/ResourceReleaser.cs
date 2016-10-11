using FBCore.Common.References;
using System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// ResourceReleaser helper class
    /// </summary>
    public class ResourceReleaserHelper<T> : IResourceReleaser<T>
    {
        private readonly Action<T> _func;

        /// <summary>
        /// Instantiates the <see cref="ResourceReleaserHelper&lt;T&gt;"/>.
        /// </summary>
        /// <param name="func">Delegate function</param>
        public ResourceReleaserHelper(Action<T> func)
        {
            _func = func;
        }

        /// <summary>
        /// Invokes the Release method of the pool
        /// </summary>
        /// <param name="value">T</param>
        public void Release(T value)
        {
            _func(value);
        }
    }
}
