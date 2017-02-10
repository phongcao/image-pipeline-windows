using FBCore.Common.References;
using FBCore.DataSource;
using ImagePipeline.Image;
using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Datasource
{
    /// <summary>
    /// Provides the custom implementation for <see cref="BaseBitmapDataSubscriber"/>.
    /// </summary>
    public class BaseBitmapDataSubscriberImpl : BaseBitmapDataSubscriber
    {
        private Func<SoftwareBitmap, Task> _onNewResultImplFunc;
        private Action<IDataSource<CloseableReference<CloseableImage>>> _onFailureImplFunc;

        /// <summary>
        /// Instantiates the <see cref="BaseBitmapDataSubscriberImpl"/>.
        /// </summary>
        public BaseBitmapDataSubscriberImpl(
            Func<SoftwareBitmap, Task> onNewResultImplFunc,
            Action<IDataSource<CloseableReference<CloseableImage>>> onFailureImplFunc)
        {
            _onNewResultImplFunc = onNewResultImplFunc;
            _onFailureImplFunc = onFailureImplFunc;
        }

        /// <summary>
        /// Implementation for OnNewResult.
        /// </summary>
        public override Task OnNewResultImpl(SoftwareBitmap dataSource)
        {
            return _onNewResultImplFunc(dataSource);
        }

        /// <summary>
        /// Implementation for OnFailure.
        /// </summary>
        public override void OnFailureImpl(IDataSource<CloseableReference<CloseableImage>> dataSource)
        {
            _onFailureImplFunc(dataSource);
        }
    }
}
