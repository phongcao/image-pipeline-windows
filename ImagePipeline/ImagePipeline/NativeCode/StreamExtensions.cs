using System.IO;

namespace ImagePipeline.NativeCode
{
    /// <summary>
    /// Extension methods of <see cref="Stream"/>.
    /// </summary>
    internal static class StreamExtensions
    {
        /// <summary>
        /// Converts a <see cref="Stream"/> to <see cref="ManagedIStream"/>.
        /// </summary>
        /// <param name="stream">Input stream.</param>
        /// <returns>ManagedIStream wrapper.</returns>
        public static ManagedIStream AsIStream(this Stream stream)
        {
            return new ManagedIStream(stream);
        }
    }
}
