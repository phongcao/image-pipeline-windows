using ImagePipeline.Core;
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
    /// Postprocessor Sample.
    /// </summary>
    public sealed partial class PostprocessorSample : Page
    {
        private ImagePipelineCore _imagePipeline;

        public PostprocessorSample()
        {
            InitializeComponent();

            // Initializes ImagePipeline
            _imagePipeline = ImagePipelineFactory.Instance.GetImagePipeline();

            FetchButton.IsEnabled = true;
        }

        private async void FetchButton_Click(object sender, RoutedEventArgs e)
        {
            ImageGrid.Items.Clear();
            await _imagePipeline.ClearCachesAsync();
            FetchButton.IsEnabled = false;
            ImageCounter.Visibility = Visibility.Visible;
            for (int i = 0; i < MainPage.NUM_IMAGES; i++)
            {
                try
                {
                    Uri uri = MainPage.GenerateImageUri();
                    ImageRequestBuilder builder = ImageRequestBuilder.NewBuilderWithSource(uri);
                    if (GrayscaleRadioButton.IsChecked.Value)
                    {
                        builder.SetPostprocessor(GrayscalePostprocessor);
                    }
                    else if (InvertRadioButton.IsChecked.Value)
                    {
                        builder.SetPostprocessor(InvertPostprocessor);
                    }

                    ImageRequest request = builder.Build();
                    WriteableBitmap bitmap = await _imagePipeline.FetchDecodedBitmapImage(request);
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

            FetchButton.IsEnabled = true;
            ImageCounter.Visibility = Visibility.Collapsed;
        }

        private void ShowSliptView(object sender, RoutedEventArgs e)
        {
            MySamplesPane.SamplesSplitView.IsPaneOpen = !MySamplesPane.SamplesSplitView.IsPaneOpen;
        }

        private void InvertPostprocessor(
            byte[] data,
            int width,
            int height,
            BitmapPixelFormat format,
            BitmapAlphaMode alpha)
        {
            byte value = 255;
            var stride = 4 * width;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int k = stride * i + 4 * j;
                    data[k + 0] = (byte)(value - data[k]);
                    data[k + 1] = (byte)(value - data[k + 1]);
                    data[k + 2] = (byte)(value - data[k + 2]);
                    data[k + 3] = 255;
                }
            }
        }

        private void GrayscalePostprocessor(
            byte[] data,
            int width,
            int height,
            BitmapPixelFormat format,
            BitmapAlphaMode alpha)
        {
            var stride = 4 * width;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int k = stride * i + 4 * j;
                    int grayScale = (int)((data[k + 2] * 0.3) + (data[k + 1] * 0.59) + (data[k] * 0.11));
                    data[k] = (byte)grayScale;
                    data[k + 1] = (byte)grayScale;
                    data[k + 2] = (byte)grayScale;
                    data[k + 3] = 255;
                }
            }
        }
    }
}
