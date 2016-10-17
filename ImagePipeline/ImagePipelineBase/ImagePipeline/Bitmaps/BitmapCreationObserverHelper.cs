using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Bitmaps
{
    /// <summary>
    /// BitmapCreationObserver helper class
    /// </summary>
    public class BitmapCreationObserverHelper : IBitmapCreationObserver
    {
        private readonly Action<SoftwareBitmap, object> _func;

        /// <summary>
        /// Instantiates the <see cref="BitmapCreationObserverHelper"/>.
        /// </summary>
        /// <param name="func">Delegate function</param>
        public BitmapCreationObserverHelper(Action<SoftwareBitmap, object> func)
        {
            _func = func;
        }

        /// <summary>
        /// Invokes the OnBitmapCreated method
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="callerContext"></param>
        public void OnBitmapCreated(SoftwareBitmap bitmap, object callerContext)
        {
            _func(bitmap, callerContext);
        }
    }
}
