using FBCore.Common.References;
using FBCore.DataSource;
using ImagePipeline.Image;
using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Datasource
{
    /// <summary>
    /// Provides custom implementation for <see cref="BaseBitmapDataSubscriber"/>.
    /// </summary>
    public class BaseBitmapDataSubscriberImpl : BaseBitmapDataSubscriber
    {
        private Action<SoftwareBitmap> _onNewResultImplFunc;
        private Action<IDataSource<CloseableReference<CloseableImage>>> _onFailureImplFunc;

        /// <summary>
        /// Instantiates the <see cref="BaseBitmapDataSubscriberImpl"/>
        /// </summary>
        /// <param name="onNewResultImplFunc"></param>
        /// <param name="onFailureImplFunc"></param>
        public BaseBitmapDataSubscriberImpl(
            Action<SoftwareBitmap> onNewResultImplFunc,
            Action<IDataSource<CloseableReference<CloseableImage>>> onFailureImplFunc)
        {
            _onNewResultImplFunc = onNewResultImplFunc;
            _onFailureImplFunc = onFailureImplFunc;
        }

        /// <summary>
        /// Implementation for OnNewResult
        /// </summary>
        /// <param name="dataSource"></param>
        public override void OnNewResultImpl(SoftwareBitmap dataSource)
        {
            _onNewResultImplFunc(dataSource);
        }

        /// <summary>
        /// Implementation for OnFailure
        /// </summary>
        /// <param name="dataSource"></param>
        public override void OnFailureImpl(IDataSource<CloseableReference<CloseableImage>> dataSource)
        {
            _onFailureImplFunc(dataSource);
        }
    }
}
