namespace ImagePipeline.Common
{
    /// <summary>
    /// Options for changing the behavior of the ImageDecoder.
    /// </summary>
    public class ImageDecodeOptions
    {
        private static readonly ImageDecodeOptions DEFAULTS = ImageDecodeOptions.NewBuilder().Build();

        /// <summary>
        /// Decoding of intermediate results for an image won't happen more often that minDecodeIntervalMs.
        /// </summary>
        public int MinDecodeIntervalMs { get; }

        /// <summary>
        /// Whether to decode a preview frame for animated images.
        /// </summary>
        public bool DecodePreviewFrame { get; }

        /// <summary>
       /// Indicates that the last frame should be used as the preview frame instead of the first.
       /// </summary>
        public bool UseLastFrameForPreview { get; }

        /// <summary>
       /// Whether to decode all the frames and store them in memory. This should only ever be used
       /// for animations that are known to be small (e.g. stickers). Caching dozens of large Bitmaps
       /// in memory for general GIFs or WebP's will not fit in memory.
       /// </summary>
        public bool DecodeAllFrames { get; }

        /// <summary>
       /// Force image to be rendered as a static image, even if it is an animated format.
       ///
       /// This flag will force animated GIFs to be rendered as static images
       /// </summary>
        public bool ForceStaticImage { get; }

        /// <summary>
        /// Instantiates the <see cref="ImageDecodeOptions"/>
        /// </summary>
        /// <param name="b"></param>
        public ImageDecodeOptions(ImageDecodeOptionsBuilder b)
        {
            MinDecodeIntervalMs = b.MinDecodeIntervalMs;
            DecodePreviewFrame = b.DecodePreviewFrame;
            UseLastFrameForPreview = b.UseLastFrameForPreview;
            DecodeAllFrames = b.DecodeAllFrames;
            ForceStaticImage = b.ForceStaticImage;
        }

        /// <summary>
        /// Gets the default options.
        ///
        /// @return the default options
        /// </summary>
        public static ImageDecodeOptions Defaults
        {
            get
            {
                return DEFAULTS;
            }
        }

        /// <summary>
        /// Creates a new builder.
        ///
        /// @return a new builder
        /// </summary>
        public static ImageDecodeOptionsBuilder NewBuilder()
        {
            return new ImageDecodeOptionsBuilder();
        }

        /// <summary>
        /// Custom Equals method
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }

            if (o == null || GetType() != o.GetType())
            {
                return false;
            }

            ImageDecodeOptions that = (ImageDecodeOptions)o;

            if (DecodePreviewFrame != that.DecodePreviewFrame)
            {
                return false;
            }

            if (UseLastFrameForPreview != that.UseLastFrameForPreview)
            {
                return false;
            }

            if (DecodeAllFrames != that.DecodeAllFrames)
            {
                return false;
            }

            if (ForceStaticImage != that.ForceStaticImage)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates the hash code basing on properties
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int result = MinDecodeIntervalMs;
            result = 31 * result + (DecodePreviewFrame ? 1 : 0);
            result = 31 * result + (UseLastFrameForPreview ? 1 : 0);
            result = 31 * result + (DecodeAllFrames ? 1 : 0);
            result = 31 * result + (ForceStaticImage ? 1 : 0);
            return result;
        }

        /// <summary>
        /// Provides the custom ToString method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(
                "{0}-{1:B}-{2:B}-{3:B}-{4:B}",
                MinDecodeIntervalMs,
                DecodePreviewFrame,
                UseLastFrameForPreview,
                DecodeAllFrames,
                ForceStaticImage);
        }
    }
}
