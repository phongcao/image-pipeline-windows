using System;
using FBCore.Common.Internal;
using FBCore.Common.References;
using FBCore.Concurrency;
using ImagePipeline.Bitmaps;
using ImagePipeline.Image;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Runs a caller-supplied post-processor object.
    ///
    /// <para />Post-processors are only supported for static bitmaps. If the request 
    /// is for an animated image, the post-processor step will be skipped without warning.
    /// </summary>
    public class PostprocessorProducer : IProducer<CloseableReference<CloseableImage>>
    {
        internal const string NAME = "PostprocessorProducer";
        internal const string POSTPROCESSOR = "Postprocessor";

        private IProducer<CloseableReference<CloseableImage>> _inputProducer;
        private PlatformBitmapFactory _bitmapFactory;
        private IExecutorService _executor;

        /// <summary>
        /// Instantiates the <see cref="PostprocessorProducer"/>
        /// </summary>
        public PostprocessorProducer(
            IProducer<CloseableReference<CloseableImage>> inputProducer,
            PlatformBitmapFactory platformBitmapFactory,
            IExecutorService executor)
        {
            _inputProducer = Preconditions.CheckNotNull(inputProducer);
            _bitmapFactory = platformBitmapFactory;
            _executor = Preconditions.CheckNotNull(executor);
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<CloseableReference<CloseableImage>> consumer, 
            IProducerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
