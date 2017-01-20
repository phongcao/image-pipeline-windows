using FBCore.Concurrency;
using Windows.System.Threading;

namespace ImagePipeline.Core
{
    /// <summary>
    /// Basic implementation of <see cref="IExecutorSupplier"/>.
    ///
    /// <para /> Provides one thread pool for the CPU-bound operations and another thread pool for the
    /// IO-bound operations.
    /// </summary>
    public class DefaultExecutorSupplier : IExecutorSupplier
    {
        // Allows for simultaneous reads and writes.
        private const int NUM_IO_BOUND_THREADS = 2;
        private const int NUM_LIGHTWEIGHT_BACKGROUND_THREADS = 1;

        private readonly IExecutorService _ioBoundExecutor;
        private readonly IExecutorService _decodeExecutor;
        private readonly IExecutorService _backgroundExecutor;
        private readonly IExecutorService _lightWeightBackgroundExecutor;

        /// <summary>
        /// Instantiates the <see cref="DefaultExecutorSupplier"/>
        /// </summary>
        /// <param name="numCpuBoundThreads"></param>
        public DefaultExecutorSupplier(int numCpuBoundThreads)
        {
            _ioBoundExecutor = Executors.NewFixedThreadPool(
                "io_bound", 
                NUM_IO_BOUND_THREADS, 
                WorkItemPriority.Normal,
                _ => {});

            _decodeExecutor = Executors.NewFixedThreadPool(
                "decode",
                numCpuBoundThreads,
                WorkItemPriority.Low,
                _ => {});

            _backgroundExecutor = Executors.NewFixedThreadPool(
                "background",
                numCpuBoundThreads,
                WorkItemPriority.Low,
                _ => {});

            _lightWeightBackgroundExecutor = Executors.NewFixedThreadPool(
                "lightweight_background",
                NUM_LIGHTWEIGHT_BACKGROUND_THREADS,
                WorkItemPriority.Low,
                _ => {});
        }

        /// <summary>
        /// Executor used to do all disk reads, whether for disk cache or local files.
        /// </summary>
        public IExecutorService ForLocalStorageRead
        {
            get
            {
                return _ioBoundExecutor;
            }
        }

        /// <summary>
        /// Executor used to do all disk writes, whether for disk cache or local files.
        /// </summary>
        /// <returns></returns>
        public IExecutorService ForLocalStorageWrite
        {
            get
            {
                return _ioBoundExecutor;
            }
        }

        /// <summary>
        /// Executor used for all decodes.
        /// </summary>
        public IExecutorService ForDecode
        {
            get
            {
                return _decodeExecutor;
            }
        }

        /// <summary>
        ///  Executor used for background tasks such as image transcoding, resizing, rotating and
        ///  post processing.
        /// </summary>
        public IExecutorService ForBackgroundTasks
        {
            get
            {
                return _backgroundExecutor;
            }
        }

        /// <summary>
        /// Executor used for lightweight background operations, such as handing request off the
        /// main thread.
        /// </summary>
        public IExecutorService ForLightweightBackgroundTasks
        {
            get
            {
                return _lightWeightBackgroundExecutor;
            }
        }
    }
}
