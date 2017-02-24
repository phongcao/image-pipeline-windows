using FBCore.Common.Internal;
using System;
using System.Diagnostics;
using System.IO;

namespace FBCore.Common.References
{
    /// <summary>
    /// DefaultResourceReleaser for CloseableReference{T}.
    /// </summary>
    public class DefaultResourceReleaser<T> : IResourceReleaser<T>
    {
        /// <summary>
        /// Default release method.
        /// </summary>
        public void Release(T value)
        {
            try
            {
                Closeables.Close((IDisposable)value, true);
            }
            catch (IOException ioe)
            {
                Debug.WriteLine($"{ ioe.Message }. This will not happen, Closeable.close swallows and logs IOExceptions.");
            }
        }
    }
}
