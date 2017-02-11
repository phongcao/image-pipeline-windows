using FBCore.Common.References;
using FBCore.Common.Util;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Remove image transform meta data producer.
    ///
    /// <para />Remove the ImageTransformMetaData object from the results passed down from 
    /// the next producer, and adds it to the result that it returns to the consumer.
    /// </summary>
    public class RemoveImageTransformMetaDataProducer :
        IProducer<CloseableReference<IRandomAccessStream>>
    {
        private readonly IProducer<EncodedImage> _inputProducer;
        private readonly FlexByteArrayPool _flexByteArrayPool;

        /// <summary>
        /// Instantiates the <see cref="RemoveImageTransformMetaDataProducer"/>
        /// </summary>
        public RemoveImageTransformMetaDataProducer(
            IProducer<EncodedImage> inputProducer,
            FlexByteArrayPool flexByteArrayPool)
        {
            _inputProducer = inputProducer;
            _flexByteArrayPool = flexByteArrayPool;
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<CloseableReference<IRandomAccessStream>> consumer, 
            IProducerContext context)
        {
            _inputProducer.ProduceResults(new RemoveImageTransformMetaDataConsumer(this, consumer), context);
        }

        private class RemoveImageTransformMetaDataConsumer : 
            DelegatingConsumer<EncodedImage, CloseableReference<IRandomAccessStream>>
        {
            private RemoveImageTransformMetaDataProducer _parent;

            internal RemoveImageTransformMetaDataConsumer(
                RemoveImageTransformMetaDataProducer parent,
                IConsumer<CloseableReference<IRandomAccessStream>> consumer) : base(consumer)
            {
                _parent = parent;
            }

            protected override void OnNewResultImpl(EncodedImage newResult, bool isLast)
            {
                CloseableReference<IRandomAccessStream> result = null;
                CloseableReference<IPooledByteBuffer> byteBufferRef = null;

                try
                {
                    if (EncodedImage.IsValid(newResult))
                    {
                        byteBufferRef = newResult.GetByteBufferRef();
                        IPooledByteBuffer byteBuffer = byteBufferRef.Get();

                        //----------------------------------------------------------------------
                        // Phong Cao: InMemoryRandomAccessStream can't write anything < 16KB.
                        // http://stackoverflow.com/questions/25928408/inmemoryrandomaccessstream-incorrect-behavior
                        //----------------------------------------------------------------------
                        int supportedSize = Math.Max(16 * ByteConstants.KB, byteBuffer.Size);

                        // Allocate temp buffer for stream convert
                        byte[] bytesArray = default(byte[]);
                        CloseableReference<byte[]> bytesArrayRef = default(CloseableReference<byte[]>);

                        try
                        {
                            bytesArrayRef = _parent._flexByteArrayPool.Get(supportedSize);
                            bytesArray = bytesArrayRef.Get();
                        }
                        catch (Exception)
                        {
                            // Allocates the byte array since the pool couldn't provide one
                            bytesArray = new byte[supportedSize];
                        }
                        finally
                        {
                            CloseableReference<byte[]>.CloseSafely(bytesArrayRef);
                        }

                        byteBuffer.Read(0, bytesArray, 0, byteBuffer.Size);
                        var outStream = new InMemoryRandomAccessStream();
                        outStream.AsStreamForWrite().Write(bytesArray, 0, supportedSize);
                        outStream.Seek(0);
                        result = CloseableReference<IRandomAccessStream>.of(outStream);
                    }

                    Consumer.OnNewResult(result, isLast);
                }
                finally
                {
                    CloseableReference<IPooledByteBuffer>.CloseSafely(byteBufferRef);
                    CloseableReference<IRandomAccessStream>.CloseSafely(result);
                }
            }
        }
    }
}
