using ImagePipeline.Core;
using ImagePipeline.Request;
using System;
using System.Collections.Generic;
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

            EnableAllButtons(true);
        }

        private async void FetchEncodedButton_Click(object sender, RoutedEventArgs e)
        {
            EnableAllButtons(false);
            ImageGrid.Items.Clear();

            for (int i = 0; i < MainPage.NUM_IMAGES; i++)
            {
                try
                {
                    Uri uri = MainPage.GenerateImageUri();
                    BitmapImage bitmap = await _imagePipeline.FetchEncodedBitmapImage(uri);
                    var image = new Image();
                    image.Width = image.Height = MainPage.VIEW_DIMENSION;
                    image.Source = bitmap;
                    ImageGrid.Items.Add(image);
                    ImageCounter.Text = string.Format("{0}/{1}", i + 1, MainPage.NUM_IMAGES);
                }
                catch (Exception)
                {
                    // Image not found, try another uri
                    --i;
                }
            }

            EnableAllButtons(true);
        }

        private async void FetchDecodedButton_Click(object sender, RoutedEventArgs e)
        {
            EnableAllButtons(false);
            ImageGrid.Items.Clear();

            for (int i = 0; i < MainPage.NUM_IMAGES; i++)
            {
                try
                {
                    Uri uri = MainPage.GenerateImageUri();
                    WriteableBitmap bitmap = await _imagePipeline.FetchDecodedBitmapImage(
                        ImageRequest.FromUri(uri));

                    var image = new Image();
                    image.Width = image.Height = MainPage.VIEW_DIMENSION;
                    image.Source = bitmap;
                    ImageGrid.Items.Add(image);
                    ImageCounter.Text = string.Format("{0}/{1}", i + 1, MainPage.NUM_IMAGES);
                }
                catch (Exception)
                {
                    // Image not found, try another uri
                    --i;
                }
            }

            EnableAllButtons(true);
        }

        private async void PrefetchButton_Click(object sender, RoutedEventArgs e)
        {
            EnableAllButtons(false);
            ImageGrid.Items.Clear();
            List<Uri> uris = new List<Uri>(MainPage.NUM_IMAGES);

            for (int i = 0; i < MainPage.NUM_IMAGES; i++)
            {
                try
                {
                    Uri uri = MainPage.GenerateImageUri();
                    await _imagePipeline.PrefetchToDiskCache(uri);
                    ImageCounter.Text = string.Format("{0}/{1}", i + 1, MainPage.NUM_IMAGES);
                    uris.Add(uri);
                }
                catch (Exception)
                {
                    // Image not found, try another uri
                    --i;
                }
            }

            foreach (var uri in uris)
            {
                BitmapImage bitmap = await _imagePipeline.FetchEncodedBitmapImage(uri);
                var image = new Image();
                image.Width = image.Height = MainPage.VIEW_DIMENSION;
                image.Source = bitmap;
                ImageGrid.Items.Add(image);
            }

            EnableAllButtons(true);
        }

        private async void EnableAllButtons(bool enable)
        {
            FetchEncodedButton.IsEnabled = enable;
            FetchDecodedButton.IsEnabled = enable;
            PrefetchButton.IsEnabled = enable;
            ImageCounter.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;
            if (!enable)
            {
                await _imagePipeline.ClearCachesAsync();
            }
        }

        private void ShowSliptView(object sender, RoutedEventArgs e)
        {
            MySamplesPane.SamplesSplitView.IsPaneOpen = !MySamplesPane.SamplesSplitView.IsPaneOpen;
        }
    }
}
