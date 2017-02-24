using FBCore.Common.References;
using FBCore.DataSource;
using ImagePipeline.Image;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Datasource
{
    /// <summary>
    /// Implementation of <see cref="IDataSubscriber{T}"/> for cases where
    /// the client wants to access a list of bitmaps.
    ///
    /// <para />
    /// Sample usage:
    /// <code>
    ///   dataSource.Subscribe(
    ///     new BaseListBitmapDataSubscriber() 
    ///     {
    ///       public void OnNewResultImpl(IList{SoftwareBitmap} bitmapList) 
    ///       {
    ///         // Pass bitmap list to system, which makes a copy of it.
    ///         Update(bitmapList);
    ///         // No need to do any cleanup.
    ///       }
    ///
    ///       public void OnFailureImpl(IDataSource dataSource) 
    ///       {
    ///         // No cleanup required here.
    ///       }
    ///     }
    /// </code>
    /// </summary>
    public abstract class BaseListBitmapDataSubscriber : 
        BaseDataSubscriber<IList<CloseableReference<CloseableImage>>>
    {
        /// <summary>
        /// Called whenever a new value is ready to be retrieved from
        /// the IDataSource.
        /// </summary>
        public override async Task OnNewResultImpl(
            IDataSource<IList<CloseableReference<CloseableImage>>> dataSource)
        {
            if (!dataSource.IsFinished())
            {
                return;
            }

            IList<CloseableReference<CloseableImage>> imageRefList = dataSource.GetResult();
            if (imageRefList == null)
            {
                await OnNewResultListImpl(null).ConfigureAwait(false);
                return;
            }

            try
            {
                IList<SoftwareBitmap> bitmapList = new List<SoftwareBitmap>(imageRefList.Count);
                foreach (var closeableImageRef in imageRefList)
                {
                    if (closeableImageRef != null && 
                        closeableImageRef.Get().GetType() == typeof(CloseableBitmap))
                    {
                        bitmapList.Add(((CloseableBitmap)closeableImageRef.Get()).UnderlyingBitmap);
                    }
                    else
                    {
                        //This is so that client gets list with same length
                        bitmapList.Add(null);
                    }
                }

                await OnNewResultListImpl(bitmapList).ConfigureAwait(false);              
            }
            finally
            {
                foreach (var closeableImageRef in imageRefList)
                {
                    CloseableReference<CloseableImage>.CloseSafely(closeableImageRef);
                }
            }
        }

        /// <summary>
       /// The bitmap list provided to this method is only guaranteed to be
       /// around for the lifespan of the method. This list can be null or
       /// the elements in it can be null.
       ///
       /// <para />The framework will free the bitmaps in the list from
       /// memory after this method has completed.
       /// </summary>
        protected abstract Task OnNewResultListImpl(IList<SoftwareBitmap> bitmapList);
    }
}
