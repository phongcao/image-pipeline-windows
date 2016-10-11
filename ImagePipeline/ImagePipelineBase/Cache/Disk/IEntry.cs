using BinaryResource;

namespace Cache.Disk
{
    /// <summary>
    /// Entry interface
    /// </summary>
    public interface IEntry
    {
        /// <summary>
        /// The id representing the resource
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Calculated on first time and never changes so it can be used as immutable
        /// </summary>
        /// <returns></returns>
        long Timestamp { get; }

        /// <summary>
        /// Calculated on first time and never changes so it can be used as immutable
        /// </summary>
        /// <returns></returns>
        long GetSize();

        /// <summary>
        /// Gets the resoure
        /// </summary>
        IBinaryResource Resource { get; }
    }
}
