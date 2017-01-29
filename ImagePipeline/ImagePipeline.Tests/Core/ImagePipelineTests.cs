using FBCore.Common.References;
using FBCore.Concurrency;
using FBCore.DataSource;
using ImagePipeline.Core;
using ImagePipeline.Memory;
using ImagePipeline.Request;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Threading;

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
        private const int MAX_DEGREE_OF_PARALLELISM = 10;

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
        [TestMethod, Timeout(30000)]
        public void Test1()
        {
            var completion = new ManualResetEvent(false);
            ImagePipelineFactory.Initialize();
            var imagePipeline = ImagePipelineFactory.Instance.GetImagePipeline();
            var imageRequest = ImageRequestBuilder
                .NewBuilderWithSource(IMAGE_URL)
                .SetProgressiveRenderingEnabled(false)
                .Build();

            IDataSource<CloseableReference<IPooledByteBuffer>>
                dataSource = imagePipeline.FetchEncodedImage(imageRequest, new object());

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
                            completion.Set();
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
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, Executors.NewFixedThreadPool(MAX_DEGREE_OF_PARALLELISM));

            // Wait for callback
            completion.WaitOne();
        }
    }
}
