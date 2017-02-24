using System;

namespace ImagePipeline.Common
{
    /// <summary>
    /// Being throwed when there are too many bitmaps in the pool.
    /// </summary>
    public class TooManyBitmapsException : Exception
    {
    }
}
