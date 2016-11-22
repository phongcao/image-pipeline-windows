using FBCore.Common.References;
using System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Provides custom implementation for <see cref="IResourceReleaser{T}"/>
    /// </summary>
    public class ResourceReleaserImpl<T> : IResourceReleaser<T>
    {
        private readonly Action<T> _func;

        /// <summary>
        /// Instantiates the <see cref="ResourceReleaserImpl{T}"/>.
        /// </summary>
        /// <param name="func">Delegate function</param>
        public ResourceReleaserImpl(Action<T> func)
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
