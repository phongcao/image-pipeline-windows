namespace ImagePipeline.AnimatedFactory
{
    /// <summary>
    /// Animated factory.
    /// </summary>
    public interface IAnimatedFactory
    {
        /// <summary>
        /// Gets the animated image factory
        /// </summary>
        /// <returns></returns>
        IAnimatedImageFactory GetAnimatedImageFactory();
    }
}
