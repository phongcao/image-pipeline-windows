using FBCore.Common.References;
using FBCore.Concurrency;
using FBCore.DataSource;
using ImagePipeline.Core;
using ImagePipeline.Request;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Threading;
using Windows.Storage.Streams;

namespace ImagePipeline.Tests.Core
{
    /// <summary>
    /// Tests for ImagePipeline.
    /// </summary>
    [TestClass]
    public class ImagePipelineTests
    {
        private readonly Uri IMAGE_URL = new Uri("http://i.imgur.com/9rkjHkK.jpg");
        private readonly Uri FAILURE_URL = new Uri("https://httpbin.org/image_not_found.png");

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
        }

        /// <summary>
        /// Test 1.
        /// </summary>
        [TestMethod, Timeout(3000)]
        public void Test1()
        {
            var completion = new ManualResetEvent(false);
            var imagePipeline = ImagePipelineFactory.Instance.GetImagePipeline();
            var imageRequest = ImageRequestBuilder
                .NewBuilderWithSource(IMAGE_URL)
                .SetProgressiveRenderingEnabled(false)
                .Build();

            IDataSource<CloseableReference<IRandomAccessStream>>
                dataSource = imagePipeline.FetchEncodedImage(imageRequest, new object());

            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IRandomAccessStream>>(
                response =>
                {
                    if (!response.IsFinished())
                    {
                        // if we are not interested in the intermediate images,
                        // we can just return here.
                        return;
                    }

                    CloseableReference<IRandomAccessStream> reference = response.GetResult();
                    if (reference != null)
                    {
                        try
                        {
                            // Do something with the result
                            using (var result = reference.Get())
                            {
                                completion.Set();
                            }
                        }
                        finally
                        {
                            CloseableReference<IRandomAccessStream>.CloseSafely(reference);
                        }
                    }
                },
                response =>
                {
                    Exception error = response.GetFailureCause();
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);

            // Wait for callback
            completion.WaitOne();
        }
    }
}
