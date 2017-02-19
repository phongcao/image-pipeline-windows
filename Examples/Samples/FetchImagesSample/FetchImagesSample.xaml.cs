using ImagePipeline.Core;
using ImagePipeline.Request;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Examples
{
    /// <summary>
    /// Fetch Image Sample.
    /// </summary>
    public sealed partial class FetchImagesSample : Page
    {
        private ImagePipelineCore _imagePipeline;

        public FetchImagesSample()
        {
            InitializeComponent();

            // Initializes ImagePipeline
            _imagePipeline = ImagePipelineFactory.Instance.GetImagePipeline();
        }

        private async void FetchEncodedImage()
        {
            try
            {
                Uri uri = MainPage.GenerateImageUri();
                BitmapImage bitmap = await _imagePipeline.FetchEncodedBitmapImage(uri);
                UpdateImageGrid(bitmap);
            }
            catch (Exception)
            {
                // Invalid uri, try again
                FetchEncodedImage();
            }
        }

        private async void FetchDecodedImage()
        {
            try
            {
                Uri uri = MainPage.GenerateImageUri();
                WriteableBitmap bitmap = await _imagePipeline.FetchDecodedBitmapImage(
                    ImageRequest.FromUri(uri));

                UpdateImageGrid(bitmap);
            }
            catch (Exception)
            {
                // Invalid uri, try again
                FetchDecodedImage();
            }
        }

        private async void PrefetchImage()
        {
            try
            {
                Uri uri = MainPage.GenerateImageUri();
                await _imagePipeline.PrefetchToDiskCache(uri);
            }
            catch (Exception)
            {
                // Invalid uri, try again
                PrefetchImage();
            }
        }

        private void UpdateImageGrid(BitmapSource source)
        {
            var image = new Image();
            image.Width = image.Height = MainPage.VIEW_DIMENSION;
            image.Source = source;
            ImageGrid.Items.Add(image);
        }

        private void FetchEncodedButton_Click(object sender, RoutedEventArgs e)
        {
            ImageGrid.Items.Clear();

            for (int i = 0; i < MainPage.NUM_IMAGES; i++)
            {
                FetchEncodedImage();
            }
        }

        private void FetchDecodedButton_Click(object sender, RoutedEventArgs e)
        {
            ImageGrid.Items.Clear();

            for (int i = 0; i < MainPage.NUM_IMAGES; i++)
            {
                FetchDecodedImage();
            }
        }

        private void PrefetchButton_Click(object sender, RoutedEventArgs e)
        {
            ImageGrid.Items.Clear();

            for (int i = 0; i < MainPage.NUM_IMAGES; i++)
            {
                PrefetchImage();
            }
        }

        private async void ClearCachesButton_Click(object sender, RoutedEventArgs e)
        {
            await _imagePipeline.ClearCachesAsync().ConfigureAwait(false);
        }

        private void ShowSliptView(object sender, RoutedEventArgs e)
        {
            MySamplesPane.SamplesSplitView.IsPaneOpen = !MySamplesPane.SamplesSplitView.IsPaneOpen;
        }
    }
}
