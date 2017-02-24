namespace ImageUtils
{
    /// <summary>
    /// This is a class for reading and writing Exif tags in a JPEG file.
    /// </summary>
    public class ExifInterface
    {
        /// <summary>
        /// Undefined constant.
        /// </summary>
        public const int ORIENTATION_UNDEFINED = 0;

        /// <summary>
        /// Normal constant.
        /// </summary>
        public const int ORIENTATION_NORMAL = 1;

        /// <summary>
        /// Flip horizontal constant.
        /// </summary>
        public const int ORIENTATION_FLIP_HORIZONTAL = 2; // left right reversed mirror

        /// <summary>
        /// Rotate 180 degrees constant.
        /// </summary>
        public const int ORIENTATION_ROTATE_180 = 3;

        /// <summary>
        /// Flip vertical constant.
        /// </summary>
        public const int ORIENTATION_FLIP_VERTICAL = 4; // upside down mirror

        /// <summary>
        /// Transpose constant.
        /// </summary>
        public const int ORIENTATION_TRANSPOSE = 5; // flipped about top-left <--> bottom-right axis

        /// <summary>
        /// Rotate 90 degrees constant.
        /// </summary>
        public const int ORIENTATION_ROTATE_90 = 6; // rotate 90 cw to right it

        /// <summary>
        /// Transverse constant.
        /// </summary>
        public const int ORIENTATION_TRANSVERSE = 7; // flipped about top-right <--> bottom-left axis

        /// <summary>
        /// Rotate 270 degrees constant.
        /// </summary>
        public const int ORIENTATION_ROTATE_270 = 8; // rotate 270 to right it
    }
}
