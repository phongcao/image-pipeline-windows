using System;

namespace ImagePipeline.Memory
{
    /**
     * An exception to indicate that the requested size was invalid
     */
    class InvalidSizeException : Exception
    {
        public InvalidSizeException(object size) : base("Invalid size: " + size.ToString())
        {
        }
    }
}
