namespace Cache.Disk
{
    /// <summary>
    /// Disk dump info entry
    /// </summary>
    public class DiskDumpInfoEntry
    {
        /// <summary>
        /// Gets entry path
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets entry type
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets entry size
        /// </summary>
        public float Size { get; }

        /// <summary>
        /// Gets entry first bits
        /// </summary>
        public string FirstBits { get; }

        /// <summary>
        /// Instantiates the <see cref="DiskDumpInfoEntry"/>
        /// </summary>
        public DiskDumpInfoEntry(string path, string type, float size, string firstBits)
        {
            Path = path;
            Type = type;
            Size = size;
            FirstBits = firstBits;
        }
    }
}
