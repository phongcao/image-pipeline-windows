using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Bitmaps
{
    /// <summary>
    /// Provides custom implementation for <see cref="IBitmapCreationObserver" />.
    /// </summary>
    public class BitmapCreationObserverImpl : IBitmapCreationObserver
    {
        private readonly Action<SoftwareBitmap, object> _func;

        /// <summary>
        /// Instantiates the <see cref="BitmapCreationObserverImpl"/>.
        /// </summary>
        /// <param name="func">Delegate function.</param>
        public BitmapCreationObserverImpl(Action<SoftwareBitmap, object> func)
        {
            _func = func;
        }

        /// <summary>
        /// Invokes the OnBitmapCreated method.
        /// </summary>
        public void OnBitmapCreated(SoftwareBitmap bitmap, object callerContext)
        {
            _func(bitmap, callerContext);
        }
    }
}
