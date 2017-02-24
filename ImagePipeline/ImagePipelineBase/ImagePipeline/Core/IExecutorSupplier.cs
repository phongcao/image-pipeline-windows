using FBCore.Concurrency;

namespace ImagePipeline.Core
{
    /// <summary>
    /// Implementations of this interface are responsible for supplying the
    /// different executors used by different sections of the image pipeline.
    ///
    /// <para />A very basic implementation would supply a single thread pool
    /// for all four operations. It is recommended that
    /// <see cref="ForLocalStorageRead"/> and <see cref="ForLocalStorageWrite"/>
    /// at least be different, as their threads will be I/O-bound, rather than
    /// CPU-bound as the others are.
    ///
    /// <para />Implementations should return singleton objects from these
    /// methods.
    ///
    /// </summary>
    public interface IExecutorSupplier
    {
        /// <summary>
        /// Executor used to do all disk reads, whether for disk cache or
        /// local files.
        /// </summary>
        IExecutorService ForLocalStorageRead { get; }

        /// <summary>
        /// Executor used to do all disk writes, whether for disk cache or
        /// local files.
        /// </summary>
        IExecutorService ForLocalStorageWrite { get; }

        /// <summary>
        /// Executor used for all decodes. 
        /// </summary>
        IExecutorService ForDecode { get; }

        /// <summary>
        /// Executor used for background tasks such as image transcoding,
        /// resizing, rotating and post processing.
        /// </summary>
        IExecutorService ForBackgroundTasks { get; }

        /// <summary>
        /// Executor used for lightweight background operations, such as
        /// handing request off the main thread.
        /// </summary>
        IExecutorService ForLightweightBackgroundTasks { get; }
    }
}
