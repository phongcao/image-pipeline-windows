using System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Indicates that the pool size will exceed the hard cap if we
    /// allocated a value of size 'allocSize'.
    /// </summary>
    class PoolSizeViolationException : Exception
    {
        public PoolSizeViolationException(int hardCap, int usedBytes, int freeBytes, int allocSize) : 
            base("Pool hard cap violation?" + " Hard cap = " + hardCap + " Used size = " + usedBytes + " Free size = " + freeBytes + " Request size = " + allocSize)
        {
        }
    }
}
