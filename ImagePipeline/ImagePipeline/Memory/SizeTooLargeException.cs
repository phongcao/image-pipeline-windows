namespace ImagePipeline.Memory
{
    /**
     * A specific case of InvalidSizeException used to indicate that the requested size was too large
     */
    class SizeTooLargeException : InvalidSizeException
    {
        public SizeTooLargeException(object size) : base(size)
        {
        }
    }
}
