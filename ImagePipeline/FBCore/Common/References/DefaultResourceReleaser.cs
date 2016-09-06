using FBCore.Common.Internal;
using System;
using System.IO;

namespace FBCore.Common.References
{
    public class DefaultResourceReleaser : IResourceReleaser<IDisposable>
    {
        public void Release(IDisposable value)
        {
            try
            {
                Closeables.Close(value, true);
            }
            catch (IOException ioe)
            {
                // This will not happen, Closeable.close swallows and logs IOExceptions
            }
        }
    }
}
