using System.Collections.Generic;

namespace Cache.Disk
{
    /// <summary>
    /// Disk dump info.
    /// </summary>
    public class DiskDumpInfo
    {
        /// <summary>
        /// Gets all disk dump info entries.
        /// </summary>
        public IList<DiskDumpInfoEntry> Entries { get; }

        /// <summary>
        /// Gets type count.
        /// </summary>
        public IDictionary<string, int> TypeCounts { get; }

        /// <summary>
        /// Instantiates the <see cref="DiskDumpInfo"/>.
        /// </summary>
        public DiskDumpInfo()
        {
            Entries = new List<DiskDumpInfoEntry>();
            TypeCounts = new Dictionary<string, int>();
        }
    }
}
