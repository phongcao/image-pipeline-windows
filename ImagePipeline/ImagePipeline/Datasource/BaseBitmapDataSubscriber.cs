using FBCore.Common.References;
using FBCore.DataSource;
using ImagePipeline.Image;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Datasource
{
    /// <summary>
    /// Implementation of <see cref="IDataSubscriber{T}"/> for cases where the client 
    /// wants access to a bitmap.
    ///
    /// <para />
    /// Sample usage:
    /// 
    /// <code>
    /// dataSource.Subscribe(
    ///   new BaseBitmapDataSubscriber() {
    ///     public void OnNewResultImpl(SoftwareBitmap bitmap) 
    ///     {
    ///       // Pass bitmap to system, which makes a copy of the bitmap.
    ///       UpdateStatus(bitmap);
    ///       // No need to do any cleanup.
    ///     }
    ///
    ///     public void OnFailureImpl(IDataSource dataSource) 
    ///     {
    ///       // No cleanup required here.
    ///     }
    ///   });
    /// </code>
    /// 
    /// </summary>
    public abstract class BaseBitmapDataSubscriber : BaseDataSubscriber<CloseableReference<CloseableImage>>
    {
        /// <summary>
        /// Called whenever a new value is ready to be retrieved from the DataSource.
        /// </summary>
        /// <param name="dataSource"></param>
        public override void OnNewResultImpl(IDataSource<CloseableReference<CloseableImage>> dataSource)
        {
            if (!dataSource.IsFinished())
            {
                return;
            }

            CloseableReference<CloseableImage> closeableImageRef = dataSource.GetResult();
            SoftwareBitmap bitmap = null;
            if (closeableImageRef != null &&
                closeableImageRef.Get().GetType() == typeof(CloseableBitmap))
            {
                bitmap = ((CloseableBitmap)closeableImageRef.Get()).UnderlyingBitmap;
            }

            try
            {
                OnNewResultImpl(bitmap);
            }
            finally
            {
                CloseableReference<CloseableImage>.CloseSafely(closeableImageRef);
            }
        }

        /// <summary>
        /// The bitmap provided to this method is only guaranteed to be around for the lifespan of the
        /// method.
        ///
        /// <para />The framework will free the bitmap's memory after this method has completed.
        /// <param name="bitmap"></param>
        /// </summary>
        protected abstract void OnNewResultImpl(SoftwareBitmap bitmap);
    }
}
