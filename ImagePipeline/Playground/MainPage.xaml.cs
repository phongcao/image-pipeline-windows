using FBCore.Common.References;
using FBCore.Concurrency;
using FBCore.DataSource;
using ImagePipeline.Core;
using ImagePipeline.Memory;
using ImagePipeline.Request;
using System;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Playground
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly Uri IMAGE_URL = new Uri("http://i.imgur.com/9rkjHkK.jpg");

        public MainPage()
        {
            InitializeComponent();

            var imagePipeline = ImagePipelineFactory.Instance.GetImagePipeline();
            var imageRequest = ImageRequestBuilder
                .NewBuilderWithSource(IMAGE_URL)
                .SetProgressiveRenderingEnabled(false)
                .Build();

            var dataSource = imagePipeline.FetchEncodedImage(imageRequest, new object());
            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    if (!response.IsFinished())
                    {
                        // if we are not interested in the intermediate images,
                        // we can just return here.
                        return;
                    }

                    CloseableReference<IPooledByteBuffer> reference = response.GetResult();
                    if (reference != null)
                    {
                        try
                        {
                            // do something with the result
                            IPooledByteBuffer result = reference.Get();
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                        }
                    }
                },
                response =>
                {
                    Exception error = response.GetFailureCause();
                });

            dataSource.Subscribe(dataSubscriber, Executors.NewFixedThreadPool(1));
        }
    }
}
