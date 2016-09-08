using FBCore.Common.Internal;
using System;
using System.Diagnostics;
using System.IO;

namespace FBCore.Common.References
{
    /// <summary>
    /// DefaultResourceReleaser for CloseableReference
    /// </summary>
    public class DefaultResourceReleaser : IResourceReleaser<IDisposable>
    {
        /// <summary>
        /// Default release method
        /// </summary>
        public void Release(IDisposable value)
        {
            try
            {
                Closeables.Close(value, true);
            }
            catch (IOException ioe)
            {
                Debug.WriteLine($"{ ioe.Message }. This will not happen, Closeable.close swallows and logs IOExceptions.");
            }
        }
    }
}
