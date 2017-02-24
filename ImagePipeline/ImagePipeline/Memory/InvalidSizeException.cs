using System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// An exception to indicate that the requested size was invalid.
    /// </summary>
    class InvalidSizeException : Exception
    {
        public InvalidSizeException(object size) : base("Invalid size: " + size.ToString())
        {
        }
    }
}
