using ImagePipeline.Bitmaps;
using ImagePipeline.Core;

namespace ImagePipeline.AnimatedFactory
{
    /// <summary>
    /// The animated factory provider.
    /// </summary>
    public class AnimatedFactoryProvider
    {
        /// <summary>
        /// Gets the animated factory.
        /// </summary>
        public static IAnimatedFactory GetAnimatedFactory(
            PlatformBitmapFactory platformBitmapFactory,
            IExecutorSupplier executorSupplier)
        {
            // TODO: Adding animated factory
            return default(IAnimatedFactory);
        }
    }
}
