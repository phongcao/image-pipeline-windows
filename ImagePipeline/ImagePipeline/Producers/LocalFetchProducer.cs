using FBCore.Common.Internal;
using FBCore.Common.References;
using FBCore.Concurrency;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Request;
using System.IO;
using System.Threading.Tasks;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Represents a local fetch producer.
    /// </summary>
    public abstract class LocalFetchProducer : IProducer<EncodedImage>
    {
        private readonly IExecutorService _executor;
        private readonly IPooledByteBufferFactory _pooledByteBufferFactory;

        /// <summary>
        /// Instantiates the <see cref="LocalFetchProducer"/>.
        /// </summary>
        protected LocalFetchProducer(
            IExecutorService executor,
            IPooledByteBufferFactory pooledByteBufferFactory)
        {
            _executor = executor;
            _pooledByteBufferFactory = pooledByteBufferFactory;
        }

        /// <summary>
        /// Start producing results for given context.
        /// Provided consumer is notified whenever progress is made
        /// (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<EncodedImage> consumer,
            IProducerContext producerContext)
        {
            IProducerListener listener = producerContext.Listener;
            string requestId = producerContext.Id;
            ImageRequest imageRequest = producerContext.ImageRequest;
            StatefulProducerRunnable<EncodedImage> cancellableProducerRunnable =
                new StatefulProducerRunnableImpl<EncodedImage>(
                    consumer,
                    listener,
                    ProducerName,
                    requestId,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    (result) =>
                    {
                        EncodedImage.CloseSafely(result);
                    },
                    async () =>
                    {
                        EncodedImage encodedImage = await GetEncodedImage(imageRequest)
                            .ConfigureAwait(false);

                        if (encodedImage == null)
                        {
                            return null;
                        }

                        await encodedImage.ParseMetaDataAsync().ConfigureAwait(false);
                        return encodedImage;
                    });

            producerContext.AddCallbacks(
                new BaseProducerContextCallbacks(
                    () =>
                    {
                        cancellableProducerRunnable.Cancel();
                    },
                    () => { },
                    () => { },
                    () => { }));

            _executor.Execute(cancellableProducerRunnable.Runnable);
        }

        /// <summary>
        /// Creates a memory-backed encoded image from the stream.
        /// The stream is closed.
        /// </summary>
        protected EncodedImage GetByteBufferBackedEncodedImage(Stream inputStream, int length)
        {
            var reference = default(CloseableReference<IPooledByteBuffer>);
            try
            {
                if (length <= 0)
                {
                    reference = CloseableReference<IPooledByteBuffer>.of(
                        _pooledByteBufferFactory.NewByteBuffer(inputStream));
                }
                else
                {
                    reference = CloseableReference<IPooledByteBuffer>.of(
                        _pooledByteBufferFactory.NewByteBuffer(inputStream, length));
                }

                return new EncodedImage(reference);
            }
            finally
            {
                Closeables.CloseQuietly(inputStream);
                CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
            }
        }

        /// <summary>
        /// Creates an encoded image from a file stream.
        /// </summary>
        protected EncodedImage GetInputStreamBackedEncodedImage(FileSystemInfo file, int length)
        {
            ISupplier<FileStream> sup = new SupplierImpl<FileStream>(
                () =>
                {
                    try
                    {
                        return new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
                    }
                    catch (IOException)
                    {
                        throw;
                    }
                });

            return new EncodedImage(sup, length);
        }

        /// <summary>
        /// Creates an encoded image from a byte buffer.
        /// </summary>
        protected EncodedImage GetEncodedImage(Stream inputStream, int length)
        {
            return GetByteBufferBackedEncodedImage(inputStream, length);
        }

        /// <summary>
        /// Gets an encoded image from the local resource. It can be
        /// either backed by a FileStream or an IPooledByteBuffer.
        /// </summary>
        /// <param name="imageRequest">
        /// Request that includes the local resource that is being accessed.
        /// </param>
        /// <exception cref="IOException">Source uri is invalid.</exception>
        protected abstract Task<EncodedImage> GetEncodedImage(ImageRequest imageRequest);

        /// <summary>
        /// The name of the Producer
        /// </summary>
        protected abstract string ProducerName { get; }
    }
}
