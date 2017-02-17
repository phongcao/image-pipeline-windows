using FBCore.Common.References;
using FBCore.Concurrency;
using FBCore.DataSource;
using ImagePipeline.Core;
using ImagePipeline.Image;
using ImagePipeline.Platform;
using ImagePipeline.Request;
using System;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Examples
{
    /// <summary>
    /// Progressive Rendering Sample.
    /// </summary>
    public sealed partial class ProgressiveRenderingSample : Page
    {
        private static Random _rnd = new Random();
        private ImagePipelineCore _imagePipeline;

        public ProgressiveRenderingSample()
        {
            InitializeComponent();

            // Initializes ImagePipeline
            _imagePipeline = ImagePipelineFactory.Instance.GetImagePipeline();
        }

        private void Fetch_Click(object sender, RoutedEventArgs e)
        {
            var imageId = _rnd.Next(1, 100);
            _imagePipeline.ClearCaches();
            UpdateImage(Image1, new Uri($"https://unsplash.it/800/600?image={ imageId }"));
            UpdateImage(Image2, new Uri($"https://unsplash.it/800/600?image={ imageId + 1}"));
            UpdateImage(Image3, new Uri($"https://unsplash.it/800/600?image={ imageId + 2 }"));
            UpdateImage(Image4, new Uri($"https://unsplash.it/800/600?image={ imageId + 3 }"));
        }

        public void UpdateImage(Image image, Uri uri)
        {
            ImageRequest request = ImageRequestBuilder
                .NewBuilderWithSource(uri)
                .SetProgressiveRenderingEnabled(true)
                .Build();

            var dataSource = _imagePipeline.FetchDecodedImage(request, null);
            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<CloseableImage>>(
                async bitmapDataSource =>
                {
                    if (bitmapDataSource != null)
                    {
                        var reference = bitmapDataSource.GetResult();

                        try
                        {
                            SoftwareBitmap bitmap = ((CloseableBitmap)reference.Get()).UnderlyingBitmap;

                            await DispatcherHelpers.RunOnDispatcherAsync(() =>
                            {
                                var writeableBitmap = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight);
                                bitmap.CopyToBuffer(writeableBitmap.PixelBuffer);
                                image.Source = writeableBitmap;
                            })
                            .ConfigureAwait(false);
                        }
                        finally
                        {
                            CloseableReference<CloseableImage>.CloseSafely(reference);
                        }
                    }
                },
                _ => { });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
        }

        private void ShowSliptView(object sender, RoutedEventArgs e)
        {
            MySamplesPane.SamplesSplitView.IsPaneOpen = !MySamplesPane.SamplesSplitView.IsPaneOpen;
        }
    }
}
