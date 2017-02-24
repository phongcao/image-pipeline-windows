using System.IO;

namespace Cache.Common
{
    /// <summary>
    /// Callback that writes to an <see cref="Stream"/>.
    /// </summary>
    public interface IWriterCallback
    {
        /// <summary>
        /// Write to the output stream.
        /// </summary>
        /// <param name="os">The output stream.</param>
        void Write(Stream os);
    }
}
