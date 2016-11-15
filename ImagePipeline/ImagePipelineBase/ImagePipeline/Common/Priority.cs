namespace ImagePipeline.Common
{
    /// <summary>
    /// Priority levels recognized by the image pipeline.
    /// </summary>
    public class Priority
    {
        /// <summary>
        /// NOTE: DO NOT CHANGE ORDERING OF THOSE CONSTANTS UNDER ANY CIRCUMSTANCES.
        /// Doing so will make ordering incorrect.
        /// </summary>

        /// <summary>
        /// Lowest priority level. Used for prefetches of non-visible images.
        /// </summary>
        public const int LOW = 1;

        /// <summary>
        /// Medium priority level. Used for warming of images that might soon get visible.
        /// </summary>
        public const int MEDIUM = 2;

        /// <summary>
        /// Highest priority level. Used for images that are currently visible on screen.
        /// </summary>
        public const int HIGH = 3;

        /// <summary>
       /// Gets the higher priority among the two.
       /// <param name="priority1"></param>
       /// <param name="priority2"></param>
       /// @return higher priority
       /// </summary>
        public static int GetHigherPriority(
            int priority1,
            int priority2)
        {
            return priority1 > priority2 ? priority1 : priority2;
        }
    }
}
