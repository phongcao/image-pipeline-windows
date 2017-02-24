using System;

namespace ImagePipeline.Decoder
{
    /// <summary>
    /// Decode exception.
    /// </summary>
    public class DecodeException : Exception
    {
        /// <summary>
        /// Instantiates the <see cref="DecodeException"/>.
        /// </summary>
        public DecodeException(string message) : 
            base(message)
        {
        }

        /// <summary>
        /// Instantiates the <see cref="DecodeException"/>.
        /// </summary>
        public DecodeException(string message, Exception innerException) : 
            base(message, innerException)
        {
        }
    }
}
