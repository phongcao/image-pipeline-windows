using ImagePipeline.Core;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Examples
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ImagePipelineCore _imagePipeline;

        public MainPage()
        {
            InitializeComponent();

            // Initializes ImagePipeline
            _imagePipeline = ImagePipelineFactory.Instance.GetImagePipeline();
        }
    }
}
