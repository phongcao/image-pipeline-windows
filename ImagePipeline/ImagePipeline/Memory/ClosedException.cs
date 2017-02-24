using System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Exception indicating that the PooledByteBuffer is closed.
    /// </summary>
    class ClosedException : Exception
    {
        public ClosedException() : base("Invalid bytebuf. Already closed")
        {
        }
    }
}
