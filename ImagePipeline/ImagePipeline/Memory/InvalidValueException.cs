using System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// An exception to indicate if the 'value' is invalid.
    /// </summary>
    class InvalidValueException : Exception
    {
        public InvalidValueException(object value) : base("Invalid value: " + value.ToString())
        {
        }
    }
}
