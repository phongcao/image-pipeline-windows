namespace ImagePipeline.AnimatedFactory
{
    /// <summary>
    /// Animated factory.
    /// </summary>
    public interface IAnimatedFactory
    {
        /// <summary>
        /// Gets the animated image factory.
        /// </summary>
        IAnimatedImageFactory GetAnimatedImageFactory();
    }
}
