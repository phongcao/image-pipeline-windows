namespace ImagePipeline.Common
{
    /// <summary>
    /// Builder for <see cref="ImageDecodeOptions"/>.
    /// </summary>
    public class ImageDecodeOptionsBuilder
    {
        /// <summary>
        /// The minimum decode interval in milliseconds.
        /// </summary>
        public int MinDecodeIntervalMs { get; private set; } = 100;

        /// <summary>
        /// Whether to decode a preview frame for animated images.
        /// </summary>
        public bool DecodePreviewFrame { get; private set; }

        /// <summary>
        /// Whether to use the last frame for the preview image
        /// (defaults to the first frame).
        /// </summary>
        public bool UseLastFrameForPreview { get; private set; }

        /// <summary>
        /// Gets whether to decode all the frames and store them in memory.
        /// This should only ever be used for animations that are known to
        /// be small (e.g. stickers). Caching dozens of large bitmaps
        /// in memory for general GIFs or WebP's will not fit in memory.
        /// </summary>
        /// <returns>
        /// Whether to decode all the frames and store them in memory.
        /// </returns>
        public bool DecodeAllFrames { get; private set; }

        /// <summary>
        /// Gets whether to force animated image formats to be decoded
        /// as static, non-animated images.
        /// </summary>
        /// <returns>
        /// Whether to force animated image formats to be decoded as static.
        /// </returns>
        public bool ForceStaticImage { get; private set; }

        /// <summary>
        /// Instantiates the <see cref="ImageDecodeOptionsBuilder"/>
        /// </summary>
        public ImageDecodeOptionsBuilder()
        {
        }

        /// <summary>
        /// Sets the builder to be equivalent to the specified options.
        /// </summary>
        /// <param name="options">The options to copy from.</param>
        /// <returns>This builder.</returns>
        public ImageDecodeOptionsBuilder SetFrom(ImageDecodeOptions options)
        {
            DecodePreviewFrame = options.DecodePreviewFrame;
            UseLastFrameForPreview = options.UseLastFrameForPreview;
            DecodeAllFrames = options.DecodeAllFrames;
            ForceStaticImage = options.ForceStaticImage;
            return this;
        }

        /// <summary>
        /// Sets the minimum decode interval.
        ///
        /// <p/>Decoding of intermediate results won't happen more often
        /// than intervalMs. If another intermediate result comes too soon,
        /// it will be decoded only after intervalMs since the last decode.
        /// If there were more intermediate results in between, only the
        /// last one gets decoded.
        /// </summary>
        /// <param name="intervalMs">
        /// The minimum decode interval in milliseconds.
        /// </param>
        /// <returns>This builder.</returns>
        public ImageDecodeOptionsBuilder SetMinDecodeIntervalMs(int intervalMs)
        {
            MinDecodeIntervalMs = intervalMs;
            return this;
        }

        /// <summary>
        /// Sets whether to decode a preview frame for animated images.
        /// </summary>
        /// <param name="decodePreviewFrame">
        /// Whether to decode a preview frame.</param>
        /// <returns>This builder.</returns>
        public ImageDecodeOptionsBuilder SetDecodePreviewFrame(bool decodePreviewFrame)
        {
            DecodePreviewFrame = decodePreviewFrame;
            return this;
        }

        /// <summary>
        /// Sets whether to use the last frame for the preview image
        /// (defaults to the first frame).
        /// </summary>
        /// <param name="useLastFrameForPreview">
        /// Whether to use the last frame for the preview image.
        /// </param>
        /// <returns>This builder.</returns>
        public ImageDecodeOptionsBuilder SetUseLastFrameForPreview(bool useLastFrameForPreview)
        {
            UseLastFrameForPreview = useLastFrameForPreview;
            return this;
        }

        /// <summary>
        /// Sets whether to decode all the frames and store them in memory.
        /// This should only ever be used for animations that are known to
        /// be small (e.g. stickers). Caching dozens of large bitmaps
        /// in memory for general GIFs or WebP's will not fit in memory.
        /// </summary>
        /// <param name="decodeAllFrames">
        /// Whether to decode all the frames and store them in memory.
        /// </param>
        /// <returns>This builder.</returns>
        public ImageDecodeOptionsBuilder SetDecodeAllFrames(bool decodeAllFrames)
        {
            DecodeAllFrames = decodeAllFrames;
            return this;
        }

        /// <summary>
        /// Sets whether to force animated image formats to be decoded as static,
        /// non-animated images.
        /// </summary>
        /// <param name="forceStaticImage">
        /// Whether to force the image to be decoded as a static image.
        /// </param>
        /// <returns>This builder.</returns>
        public ImageDecodeOptionsBuilder SetForceStaticImage(bool forceStaticImage)
        {
            ForceStaticImage = forceStaticImage;
            return this;
        }

        /// <summary>
        /// Builds the immutable <see cref="ImageDecodeOptions"/> instance.
        /// </summary>
        /// <returns>The immutable instance.</returns>
        public ImageDecodeOptions Build()
        {
            return new ImageDecodeOptions(this);
        }
    }
}
