using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Examples
{
    public sealed partial class SamplesPane : UserControl
    {
        public SamplesPane()
        {
            this.InitializeComponent();
        }

        private void NavigateToFetchImagesSample(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(FetchImagesSample));
        }

        private void NavigateToPostProcessorSample(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(PostprocessorSample));
        }

        private void NavigateToProgressiveRenderingSample(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(ProgressiveRenderingSample));
        }

        private void NavigateToHome(object sender, RoutedEventArgs e)
        {
            ((Frame)Window.Current.Content).Navigate(typeof(MainPage));
        }
    }
}
