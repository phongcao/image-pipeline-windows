using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Request
{
    /// <summary>
    /// Provides the custom implementation for <see cref="BasePostprocessor"/>.
    /// </summary>
    public class BasePostprocessorImpl : BasePostprocessor
    {
        private Func<string> _nameFunc;
        private Action<byte[], int, int, BitmapPixelFormat, BitmapAlphaMode> _processFunc;

        /// <summary>
        /// Instantiates the <see cref="BasePostprocessorImpl"/>.
        /// </summary>
        public BasePostprocessorImpl(
            Action<byte[], int, int, BitmapPixelFormat, BitmapAlphaMode> processFunc) : 
            this(null, processFunc)
        {
        }

        /// <summary>
        /// Instantiates the <see cref="BasePostprocessorImpl"/>.
        /// </summary>
        public BasePostprocessorImpl(
            Func<string> nameFunc,
            Action<byte[], int, int, BitmapPixelFormat, BitmapAlphaMode> processFunc)
        {
            _nameFunc = nameFunc;
            _processFunc = processFunc;
        }

        /// <summary>
        /// Returns the name of this postprocessor.
        ///
        /// <para />Used for logging and analytics.
        /// </summary>
        public override string Name
        {
            get
            {
                if (_nameFunc == null)
                {
                    return base.Name;
                }
                else
                {
                    return _nameFunc();
                }
            }
        }

        /// <summary>
        /// Clients should override this method if the post-processing can be
        /// done in place.
        ///
        /// <para />The provided bitmap is a copy of the source bitmap and the
        /// implementation is free to modify it.
        /// </summary>
        /// <param name="data">The bitmap pixel data.</param>
        /// <param name="width">
        /// The width of the new software bitmap, in pixels.
        /// </param>
        /// <param name="height">
        /// The height of the new software bitmap, in pixels.
        /// </param>
        /// <param name="format">
        /// The pixel format of the new software bitmap.
        /// </param>
        /// <param name="alpha">
        /// The alpha mode of the new software bitmap.
        /// </param>
        public override void Process(
            byte[] data,
            int width,
            int height,
            BitmapPixelFormat format,
            BitmapAlphaMode alpha)
        {
            _processFunc(data, width, height, format, alpha);
        }
    }
}
