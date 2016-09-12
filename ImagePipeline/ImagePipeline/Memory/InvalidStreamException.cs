using System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// An exception indicating that this stream is no longer valid
    /// </summary>
    class InvalidStreamException : Exception
    {
        public InvalidStreamException() : base("OutputStream no longer valid")
        {
        }
    }
}
