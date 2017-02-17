using ImagePipeline.Core;
using ImagePipeline.Request;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace ImagePipeline.Tests.Core
{
    /// <summary>
    /// Tests for ImagePipeline.
    /// </summary>
    [TestClass]
    public class ImagePipelineTests
    {
        private readonly Uri IMAGE_URL = new Uri("https://unsplash.it/800/600?image=1");
        private readonly Uri IMAGE2_URL = new Uri("https://unsplash.it/800/600?image=2");
        private readonly Uri FAILURE_URL = new Uri("https://httpbin.org/image_not_found.png");

        private ImagePipelineCore _imagePipeline;
        private ImageRequestBuilder _requestBuilder;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _imagePipeline = ImagePipelineFactory.Instance.GetImagePipeline();
            _requestBuilder = ImageRequestBuilder.NewBuilderWithSource(IMAGE_URL);
        }

        /// <summary>
        /// Tests clearing caches.
        /// </summary>
        [TestMethod, Timeout(3000)]
        public async Task TestClearCaches()
        {
            var request = _requestBuilder.Build();
            await _imagePipeline.FetchDecodedBitmapImage(request).ConfigureAwait(false);
            Assert.IsTrue(_imagePipeline.IsInBitmapMemoryCache(request));
            Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(request).ConfigureAwait(false));

            await _imagePipeline.ClearCachesAsync().ConfigureAwait(false);
            Assert.IsFalse(_imagePipeline.IsInBitmapMemoryCache(request));
            Assert.IsFalse(await _imagePipeline.IsInDiskCacheAsync(request).ConfigureAwait(false));
        }

        /// <summary>
        /// Tests fetching encoded image sucessfully.
        /// </summary>
        [TestMethod, Timeout(3000)]
        public async Task TestFetchEncodedImageSuccess()
        {
            try
            {
                var image = await _imagePipeline.FetchEncodedBitmapImage(IMAGE_URL).ConfigureAwait(false);
                Assert.IsTrue(image.GetType() == typeof(BitmapImage));
                Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE_URL).ConfigureAwait(false));
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        /// <summary>
        /// Tests fetching encoded image with error.
        /// </summary>
        [TestMethod, Timeout(3000)]
        public async Task TestFetchEncodedImageFail()
        {
            try
            {
                var image = await _imagePipeline.FetchEncodedBitmapImage(FAILURE_URL).ConfigureAwait(false);
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.GetType() == typeof(IOException));
            }
        }

        /// <summary>
        /// Tests fetching decoded image sucessfully.
        /// </summary>
        [TestMethod, Timeout(3000)]
        public async Task TestFetchDecodedImageSuccess()
        {
            try
            {
                var request = ImageRequest.FromUri(IMAGE_URL);
                var image = await _imagePipeline.FetchDecodedBitmapImage(request).ConfigureAwait(false);

                Assert.IsTrue(image.GetType() == typeof(WriteableBitmap));
                Assert.IsTrue(_imagePipeline.IsInBitmapMemoryCache(request));
                Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(request).ConfigureAwait(false));
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        /// <summary>
        /// Tests fetching decoded image with error.
        /// </summary>
        [TestMethod, Timeout(3000)]
        public async Task TestFetchDecodedImageFail()
        {
            try
            {
                var image = await _imagePipeline.FetchDecodedBitmapImage(
                    ImageRequest.FromUri(FAILURE_URL)).ConfigureAwait(false);

                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.GetType() == typeof(IOException));
            }
        }

        /// <summary>
        /// Tests prefetching to bitmap cache and disk cache.
        /// </summary>
        [TestMethod, Timeout(3000)]
        public async Task TestPrefetch()
        {
            try
            {
                await _imagePipeline.PrefetchToDiskCache(IMAGE2_URL).ConfigureAwait(false);
                Assert.IsFalse(_imagePipeline.IsInBitmapMemoryCache(IMAGE2_URL));
                Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE2_URL).ConfigureAwait(false));

                await _imagePipeline.PrefetchToBitmapCache(IMAGE2_URL).ConfigureAwait(false);
                Assert.IsTrue(_imagePipeline.IsInBitmapMemoryCache(IMAGE2_URL));
                Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE2_URL).ConfigureAwait(false));

            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        /// <summary>
        /// Tests eviction from memory cache and disk cache.
        /// </summary>
        [TestMethod, Timeout(3000)]
        public async Task TestEviction()
        {
            try
            {
                // Fetch a decoded image
                await _imagePipeline.FetchDecodedBitmapImage(
                    ImageRequest.FromUri(IMAGE_URL)).ConfigureAwait(false);

                Assert.IsTrue(_imagePipeline.IsInBitmapMemoryCache(IMAGE_URL));
                Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE_URL).ConfigureAwait(false));

                // Evict from memory cache
                _imagePipeline.EvictFromMemoryCache(IMAGE_URL);
                Assert.IsFalse(_imagePipeline.IsInBitmapMemoryCache(IMAGE_URL));
                Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE_URL).ConfigureAwait(false));

                // Evict from disk cache
                await _imagePipeline.EvictFromDiskCache(IMAGE_URL).ConfigureAwait(false);
                Assert.IsFalse(_imagePipeline.IsInBitmapMemoryCache(IMAGE_URL));
                Assert.IsFalse(await _imagePipeline.IsInDiskCacheAsync(IMAGE_URL).ConfigureAwait(false));
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }
    }
}
