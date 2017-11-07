using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Examples
{
    /// <summary>
    /// Main page.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public const int NUM_IMAGES = 20;
        public const int MAX_DIMENSION = 600;
        public const int MIN_DIMENSION = 400;
        public const int VIEW_DIMENSION = 200;
        public const string IMAGE_URL = "https://picsum.photos/{0}/{1}?image={2}";

        private static Random _rnd = new Random();

        public MainPage()
        {
            InitializeComponent();
        }

        public static Uri GenerateImageUri()
        {
            var size = _rnd.Next(MIN_DIMENSION, MAX_DIMENSION);
            var imageId = _rnd.Next(1, 1000);
            return new Uri(string.Format(IMAGE_URL, size, size, imageId));
        }

        private void ShowSliptView(object sender, RoutedEventArgs e)
        {
            MySamplesPane.SamplesSplitView.IsPaneOpen = !MySamplesPane.SamplesSplitView.IsPaneOpen;
        }
    }
}
