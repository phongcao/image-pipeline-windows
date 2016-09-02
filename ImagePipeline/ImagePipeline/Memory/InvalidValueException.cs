using System;

namespace ImagePipeline.Memory
{
    /**
     * An exception to indicate if the 'value' is invalid.
     */
    class InvalidValueException : Exception
    {
        public InvalidValueException(object value) : base("Invalid value: " + value.ToString())
        {
        }
    }
}
