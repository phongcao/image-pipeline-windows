namespace ImagePipeline.Common
{
    /// <summary>
    /// Builder for <see cref="ImageDecodeOptions"/>.
    /// </summary>
    public class ImageDecodeOptionsBuilder
    {
        /// <summary>
        /// The minimum decode interval in milliseconds
        /// </summary>
        public int MinDecodeIntervalMs { get; private set; } = 100;

        /// <summary>
        /// Whether to decode a preview frame for animated images.
        /// </summary>
        public bool DecodePreviewFrame { get; private set; }

        /// <summary>
        /// Whether to use the last frame for the preview image (defaults to the first frame).
        /// </summary>
        public bool UseLastFrameForPreview { get; private set; }

        /// <summary>
        /// Gets whether to decode all the frames and store them in memory. This should only ever be used
        /// for animations that are known to be small (e.g. stickers). Caching dozens of large Bitmaps
        /// in memory for general GIFs or WebP's will not fit in memory.
        ///
        /// @return whether to decode all the frames and store them in memory
        /// </summary>
        public bool DecodeAllFrames { get; private set; }

        /// <summary>
        /// Gets whether to force animated image formats to be decoded as static, non-animated images.
        ///
        /// @return whether to force animated image formats to be decoded as static
        /// </summary>
        public bool ForceStaticImage { get; private set; }

        /// <summary>
        /// Instantiates the <see cref="ImageDecodeOptionsBuilder"/>
        /// </summary>
        public ImageDecodeOptionsBuilder()
        {
        }

        /// <summary>
        /// Sets the builder to be equivalent to the specified options.
        ///
        /// <param name="options">the options to copy from</param>
        /// @return this builder
        /// </summary>
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
        /// <p/> Decoding of intermediate results won't happen more often that intervalMs. If another
        /// intermediate result comes too soon, it will be decoded only after intervalMs since the last
        /// decode. If there were more intermediate results in between, only the last one gets decoded.
        /// <param name="intervalMs">the minimum decode interval in milliseconds</param>
        /// @return this builder
        /// </summary>
        public ImageDecodeOptionsBuilder SetMinDecodeIntervalMs(int intervalMs)
        {
            MinDecodeIntervalMs = intervalMs;
            return this;
        }

        /// <summary>
        /// Sets whether to decode a preview frame for animated images.
        ///
        /// <param name="decodePreviewFrame">whether to decode a preview frame</param>
        /// @return this builder
        /// </summary>
        public ImageDecodeOptionsBuilder SetDecodePreviewFrame(bool decodePreviewFrame)
        {
            DecodePreviewFrame = decodePreviewFrame;
            return this;
        }

        /// <summary>
        /// Sets whether to use the last frame for the preview image (defaults to the first frame).
        ///
        /// <param name="useLastFrameForPreview">whether to use the last frame for the preview image</param>
        /// @return this builder
        /// </summary>
        public ImageDecodeOptionsBuilder SetUseLastFrameForPreview(bool useLastFrameForPreview)
        {
            UseLastFrameForPreview = useLastFrameForPreview;
            return this;
        }

        /// <summary>
        /// Sets whether to decode all the frames and store them in memory. This should only ever be used
        /// for animations that are known to be small (e.g. stickers). Caching dozens of large Bitmaps
        /// in memory for general GIFs or WebP's will not fit in memory.
        ///
        /// <param name="decodeAllFrames">whether to decode all the frames and store them in memory</param>
        /// @return this builder
        /// </summary>
        public ImageDecodeOptionsBuilder SetDecodeAllFrames(bool decodeAllFrames)
        {
            DecodeAllFrames = decodeAllFrames;
            return this;
        }

        /// <summary>
        /// Sets whether to force animated image formats to be decoded as static, non-animated images.
        ///
        /// <param name="forceStaticImage">whether to force the image to be decoded as a static image</param>
        /// @return this builder
        /// </summary>
        public ImageDecodeOptionsBuilder SetForceStaticImage(bool forceStaticImage)
        {
            ForceStaticImage = forceStaticImage;
            return this;
        }

        /// <summary>
        /// Builds the immutable <see cref="ImageDecodeOptions"/> instance.
        ///
        /// @return the immutable instance
        /// </summary>
        public ImageDecodeOptions Build()
        {
            return new ImageDecodeOptions(this);
        }
    }
}
