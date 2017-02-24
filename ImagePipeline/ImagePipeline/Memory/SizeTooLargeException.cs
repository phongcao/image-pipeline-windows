namespace ImagePipeline.Memory
{
    /// <summary>
    /// A specific case of InvalidSizeException used to indicate that
    /// the requested size was too large.
    /// </summary>
    class SizeTooLargeException : InvalidSizeException
    {
        public SizeTooLargeException(object size) : base(size)
        {
        }
    }
}
