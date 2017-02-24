namespace ImagePipeline.Cache
{
    /// <summary>
    /// Interface used to get the information about the values.
    /// </summary>
    public interface IValueDescriptor<T>
    {
        /// <summary>
        /// Returns the size in bytes of the given value.
        /// </summary>
        int GetSizeInBytes(T value);
    }
}
