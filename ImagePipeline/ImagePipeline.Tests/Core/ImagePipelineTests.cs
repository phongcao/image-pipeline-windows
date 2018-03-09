﻿using FBCore.Common.References;
using FBCore.Concurrency;
using FBCore.DataSource;
using ImagePipeline.Core;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Platform;
using ImagePipeline.Request;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml.Media.Imaging;

namespace ImagePipeline.Tests.Core
{
    /// <summary>
    /// Tests for ImagePipeline.
    /// </summary>
    [TestClass]
    public class ImagePipelineTests
    {
        private readonly Uri IMAGE_URL = new Uri("https://picsum.photos/800/600?image=1");
        private readonly Uri IMAGE2_URL = new Uri("https://picsum.photos/800/600?image=2");
        private readonly Uri IMAGE3_URL = new Uri("https://picsum.photos/800/600?image=3");
        private readonly Uri IMAGE4_URL = new Uri("https://picsum.photos/800/600?image=4");
        private readonly Uri IMAGE5_URL = new Uri("https://picsum.photos/800/600?image=5");
        private readonly Uri FAILURE_URL = new Uri("https://httpbin.org/image_not_found.png");
        private readonly Uri LOCAL_PNG_URL = new Uri("ms-appx:///Assets/pngs/1.png");
        private readonly Uri LOCAL_PNG2_URL = new Uri("ms-appx:///Assets/pngs/2.png");
        private readonly Uri LOCAL_PNG3_URL = new Uri("ms-appx:///Assets/pngs/3.png");
        private readonly Uri LOCAL_PNG4_URL = new Uri("ms-appx:///Assets/pngs/4.png");
        private readonly Uri LOCAL_JPEG_URL = new Uri("ms-appx:///Assets/jpegs/1.jpeg");
        private readonly Uri LOCAL_GIF_URL = new Uri("ms-appx:///Assets/gifs/dog.gif");
        private readonly Uri LOCAL_APP_DATA_URL = new Uri("ms-appdata:///local/1.png");
        private readonly Uri LOCAL_APP_DATA2_URL = new Uri("ms-appdata:///local/2.png");
        private readonly Uri ROAMING_APP_DATA_URL = new Uri("ms-appdata:///roaming/1.png");
        private readonly Uri ROAMING_APP_DATA2_URL = new Uri("ms-appdata:///roaming/2.png");
        private readonly Uri TEMP_APP_DATA_URL = new Uri("ms-appdata:///temp/1.png");
        private readonly Uri TEMP_APP_DATA2_URL = new Uri("ms-appdata:///temp/2.png");
        private readonly string INVALID_TOKEN = "{11111111-1111-1111-1111-111111111111}";

        /// <copyright>
        /// beach.jpg file is from https://github.com/markevans/dragonfly
        /// </copyright>
        private readonly Uri LOCAL_JPEG_EXIF_URL = new Uri("ms-appx:///Assets/jpegs/beach.jpg");

        private static ImagePipelineCore _imagePipeline;
        private ImageRequestBuilder _requestBuilder;

        /// <summary>
        /// Global Initialize
        /// </summary>
        [ClassInitialize]
        public static void GlobalInitialize(TestContext testContext)
        {
            _imagePipeline = ImagePipelineFactory.Instance.GetImagePipeline();
        }

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _requestBuilder = ImageRequestBuilder.NewBuilderWithSource(IMAGE_URL);
        }

        /// <summary>
        /// Tests out clearing caches.
        /// </summary>
        [TestMethod, Timeout(5000)]
        public async Task TestClearCaches()
        {
            var request = _requestBuilder.Build();
            await _imagePipeline.FetchDecodedBitmapImageAsync(request).ConfigureAwait(false);
            Assert.IsTrue(_imagePipeline.IsInBitmapMemoryCache(request));
            Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(request).ConfigureAwait(false));

            await _imagePipeline.ClearCachesAsync().ConfigureAwait(false);
            Assert.IsFalse(_imagePipeline.IsInBitmapMemoryCache(request));
            Assert.IsFalse(await _imagePipeline.IsInDiskCacheAsync(request).ConfigureAwait(false));
        }

        /// <summary>
        /// Tests out fetching an encoded image.
        /// </summary>
        [TestMethod, Timeout(5000)]
        public void TestFetchEncodedImageSuccess()
        {
            var completion = new ManualResetEvent(false);
            var dataSource = _imagePipeline.FetchEncodedImage(ImageRequest.FromUri(IMAGE_URL), null);
            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                async response =>
                {
                    CloseableReference<IPooledByteBuffer> reference = response.GetResult();
                    if (reference != null)
                    {
                        IPooledByteBuffer inputStream = reference.Get();

                        try
                        {
                            Assert.IsTrue(inputStream.Size != 0);
                            Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE_URL).ConfigureAwait(false));
                        }
                        catch (Exception)
                        {
                            Assert.Fail();
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                            completion.Set();
                        }
                    }
                    else
                    {
                        Assert.Fail();
                        completion.Set();
                    }
                },
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching an encoded image with wrong uri.
        /// </summary>
        [TestMethod, Timeout(5000)]
        public void TestFetchEncodedImageFail()
        {
            var completion = new ManualResetEvent(false);
            var dataSource = _imagePipeline.FetchEncodedImage(ImageRequest.FromUri(FAILURE_URL), null);
            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                    return Task.CompletedTask;
                },
                response =>
                {
                    Assert.IsTrue(response.GetFailureCause().GetType() == typeof(IOException));
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching a decoded image.
        /// </summary>
        [TestMethod, Timeout(5000)]
        public void TestFetchDecodedImageSuccess()
        {
            var completion = new ManualResetEvent(false);
            var dataSource = _imagePipeline.FetchDecodedImage(ImageRequest.FromUri(IMAGE_URL), null);
            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<CloseableImage>>(
                async response =>
                {
                    CloseableReference<CloseableImage> reference = response.GetResult();
                    if (reference != null)
                    {
                        SoftwareBitmap bitmap = ((CloseableBitmap)reference.Get()).UnderlyingBitmap;

                        try
                        {
                            Assert.IsTrue(bitmap.PixelWidth != 0);
                            Assert.IsTrue(bitmap.PixelHeight != 0);
                            Assert.IsTrue(_imagePipeline.IsInBitmapMemoryCache(ImageRequest.FromUri(IMAGE_URL)));
                            Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE_URL).ConfigureAwait(false));
                        }
                        catch (Exception)
                        {
                            Assert.Fail();
                        }
                        finally
                        {
                            CloseableReference<CloseableImage>.CloseSafely(reference);
                            completion.Set();
                        }
                    }
                    else
                    {
                        Assert.Fail();
                        completion.Set();
                    }
                },
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching a decoded image with wrong uri.
        /// </summary>
        [TestMethod, Timeout(5000)]
        public void TestFetchDecodedImageFail()
        {
            var completion = new ManualResetEvent(false);
            var dataSource = _imagePipeline.FetchDecodedImage(ImageRequest.FromUri(FAILURE_URL), null);
            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<CloseableImage>>(
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                    return Task.CompletedTask;
                },
                response =>
                {
                    Assert.IsTrue(response.GetFailureCause().GetType() == typeof(IOException));
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out prefetching to the bitmap cache and disk cache.
        /// </summary>
        [TestMethod, Timeout(5000)]
        public async Task TestPrefetch()
        {
            await _imagePipeline.PrefetchToDiskCacheAsync(IMAGE2_URL).ConfigureAwait(false);
            Assert.IsFalse(_imagePipeline.IsInBitmapMemoryCache(IMAGE2_URL));
            Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE2_URL).ConfigureAwait(false));

            await _imagePipeline.PrefetchToBitmapCacheAsync(IMAGE2_URL).ConfigureAwait(false);
            Assert.IsTrue(_imagePipeline.IsInBitmapMemoryCache(IMAGE2_URL));
            Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE2_URL).ConfigureAwait(false));
        }

        /// <summary>
        /// Tests out eviction from the memory cache and disk cache.
        /// </summary>
        [TestMethod, Timeout(5000)]
        public async Task TestEviction()
        {
            // Fetch a decoded image
            await _imagePipeline.FetchDecodedBitmapImageAsync(
                ImageRequest.FromUri(IMAGE_URL)).ConfigureAwait(false);

            Assert.IsTrue(_imagePipeline.IsInBitmapMemoryCache(IMAGE_URL));
            Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE_URL).ConfigureAwait(false));

            // Evict from memory cache
            _imagePipeline.EvictFromMemoryCache(IMAGE_URL);
            Assert.IsFalse(_imagePipeline.IsInBitmapMemoryCache(IMAGE_URL));
            Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE_URL).ConfigureAwait(false));

            // Evict from disk cache
            await _imagePipeline.EvictFromDiskCacheAsync(IMAGE_URL).ConfigureAwait(false);
            Assert.IsFalse(_imagePipeline.IsInBitmapMemoryCache(IMAGE_URL));
            Assert.IsFalse(await _imagePipeline.IsInDiskCacheAsync(IMAGE_URL).ConfigureAwait(false));
        }

        /// <summary>
        /// Tests out cancelling a fetch request for an encoded image.
        /// </summary>
        [TestMethod]
        public async Task TestCancellingFetchEncodedImage1()
        {
            var failed = false;
            var dataSource = _imagePipeline.FetchEncodedImage(ImageRequest.FromUri(IMAGE3_URL), null);
            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    failed = true;
                    return Task.CompletedTask;
                },
                response =>
                {
                    failed = true;
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            dataSource.Close();
            Assert.IsFalse(failed);
            Assert.IsFalse(await _imagePipeline.IsInDiskCacheAsync(IMAGE3_URL).ConfigureAwait(false));
        }

        /// <summary>
        /// Tests out cancelling a fetch request for an encoded image.
        /// </summary>
        [TestMethod]
        public async Task TestCancellingFetchEncodedImage2()
        {
            var failed = false;
            var dataSource = _imagePipeline.FetchEncodedImage(ImageRequest.FromUri(IMAGE3_URL), null);
            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    failed = true;
                    return Task.CompletedTask;
                },
                response =>
                {
                    failed = true;
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            await Task.Delay(5).ConfigureAwait(false);
            dataSource.Close();
            Assert.IsFalse(failed);
            Assert.IsFalse(await _imagePipeline.IsInDiskCacheAsync(IMAGE3_URL).ConfigureAwait(false));
        }

        /// <summary>
        /// Tests out cancelling a fetch request for a decoded image.
        /// </summary>
        [TestMethod]
        public async Task TestCancellingFetchDecodedImage()
        {
            var failed = false;
            var dataSource = _imagePipeline.FetchDecodedImage(ImageRequest.FromUri(IMAGE3_URL), null);
            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<CloseableImage>>(
                response =>
                {
                    failed = true;
                    return Task.CompletedTask;
                },
                response =>
                {
                    failed = true;
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            dataSource.Close();
            Assert.IsFalse(failed);
            Assert.IsFalse(await _imagePipeline.IsInDiskCacheAsync(IMAGE3_URL).ConfigureAwait(false));
            Assert.IsFalse(_imagePipeline.IsInBitmapMemoryCache(ImageRequest.FromUri(IMAGE3_URL)));
        }

        /// <summary>
        /// Tests out cancelling a prefetch request to the disk cache.
        /// </summary>
        [TestMethod]
        public async Task TestCancellingPrefetchToDiskCache1()
        {
            var failed = false;
            var dataSource = _imagePipeline.PrefetchToDiskCache(ImageRequest.FromUri(IMAGE4_URL), null);
            var dataSubscriber = new BaseDataSubscriberImpl<object>(
                response =>
                {
                    failed = true;
                    return Task.CompletedTask;
                },
                response =>
                {
                    failed = true;
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            dataSource.Close();
            Assert.IsFalse(failed);
            Assert.IsFalse(await _imagePipeline.IsInDiskCacheAsync(IMAGE4_URL).ConfigureAwait(false));
        }

        /// <summary>
        /// Tests out cancelling a prefetch request to the disk cache.
        /// </summary>
        [TestMethod]
        public async Task TestCancellingPrefetchToDiskCache2()
        {
            var failed = false;
            var dataSource = _imagePipeline.PrefetchToDiskCache(ImageRequest.FromUri(IMAGE4_URL), null);
            var dataSubscriber = new BaseDataSubscriberImpl<object>(
                response =>
                {
                    failed = true;
                    return Task.CompletedTask;
                },
                response =>
                {
                    failed = true;
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            await Task.Delay(5).ConfigureAwait(false);
            dataSource.Close();
            Assert.IsFalse(failed);
            Assert.IsFalse(await _imagePipeline.IsInDiskCacheAsync(IMAGE4_URL).ConfigureAwait(false));
        }

        /// <summary>
        /// Tests out cancelling a prefetch request to the bitmap cache.
        /// </summary>
        [TestMethod]
        public async Task TestCancellingPrefetchToBitmapCache1()
        {
            var failed = false;
            var dataSource = _imagePipeline.PrefetchToBitmapCache(ImageRequest.FromUri(IMAGE5_URL), null);
            var dataSubscriber = new BaseDataSubscriberImpl<object>(
                response =>
                {
                    failed = true;
                    return Task.CompletedTask;
                },
                response =>
                {
                    failed = true;
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            dataSource.Close();
            Assert.IsFalse(failed);
            Assert.IsFalse(await _imagePipeline.IsInDiskCacheAsync(IMAGE5_URL).ConfigureAwait(false));
            Assert.IsFalse(_imagePipeline.IsInBitmapMemoryCache(ImageRequest.FromUri(IMAGE5_URL)));
        }

        /// <summary>
        /// Tests out cancelling a prefetch request to the bitmap cache.
        /// </summary>
        [TestMethod]
        public async Task TestCancellingPrefetchToBitmapCache2()
        {
            var failed = false;
            var dataSource = _imagePipeline.PrefetchToBitmapCache(ImageRequest.FromUri(IMAGE5_URL), null);
            var dataSubscriber = new BaseDataSubscriberImpl<object>(
                response =>
                {
                    failed = true;
                    return Task.CompletedTask;
                },
                response =>
                {
                    failed = true;
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            await Task.Delay(5).ConfigureAwait(false);
            dataSource.Close();
            Assert.IsFalse(failed);
            Assert.IsFalse(await _imagePipeline.IsInDiskCacheAsync(IMAGE5_URL).ConfigureAwait(false));
            Assert.IsFalse(_imagePipeline.IsInBitmapMemoryCache(ImageRequest.FromUri(IMAGE5_URL)));
        }

        /// <summary>
        /// Tests out fetching a base64 image.
        /// </summary>
        [TestMethod]
        public async Task TestFetchBase64Image()
        {
            var data = "data:image/jpeg;base64,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";
            var uri = new Uri(data);
            var image = await _imagePipeline.FetchDecodedBitmapImageAsync(
                ImageRequest.FromUri(uri)).ConfigureAwait(false);

            Assert.IsTrue(image.GetType() == typeof(WriteableBitmap));
            Assert.IsTrue(_imagePipeline.IsInBitmapMemoryCache(uri));
        }

        /// <summary>
        /// Tests out fetching a png file from local assets
        /// </summary>
        [TestMethod]
        public async Task TestFetchLocalPng()
        {
            var bitmap = await _imagePipeline.FetchDecodedBitmapImageAsync(
                ImageRequest.FromUri(LOCAL_PNG_URL)).ConfigureAwait(false);

            await DispatcherHelpers.RunOnDispatcherAsync(() =>
            {
                Assert.IsTrue(bitmap.PixelWidth != 0);
                Assert.IsTrue(bitmap.PixelHeight != 0);
            });
        }

        /// <summary>
        /// Tests out fetching a jpeg file from local assets
        /// </summary>
        [TestMethod]
        public async Task TestFetchLocalJpeg()
        {
            var bitmap = await _imagePipeline.FetchDecodedBitmapImageAsync(
                ImageRequest.FromUri(LOCAL_JPEG_URL)).ConfigureAwait(false);

            await DispatcherHelpers.RunOnDispatcherAsync(() =>
            {
                Assert.IsTrue(bitmap.PixelWidth != 0);
                Assert.IsTrue(bitmap.PixelHeight != 0);
            });
        }

        /// <summary>
        /// Tests out fetching a bitmap using file URI
        /// </summary>
        [TestMethod]
        public async Task TestFetchLocalFileUri()
        {
            // Prepare resource for testing.
            var appData = ApplicationData.Current;
            var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(LOCAL_PNG3_URL)
                .AsTask()
                .ConfigureAwait(false);

            await sourceFile.CopyAsync(appData.LocalFolder).AsTask().ConfigureAwait(false);

            var fileUri = new Uri($"file:///{ appData.LocalFolder.Path }/3.png");
            var bitmap = await _imagePipeline.FetchDecodedBitmapImageAsync(
                ImageRequest.FromUri(fileUri)).ConfigureAwait(false);

            await DispatcherHelpers.RunOnDispatcherAsync(() =>
            {
                Assert.IsTrue(bitmap.PixelWidth != 0);
                Assert.IsTrue(bitmap.PixelHeight != 0);
            });
        }

        /// <summary>
        /// Tests out fetching a bitmap from local app data folder
        /// </summary>
        [TestMethod]
        public async Task TestFetchLocalAppData()
        {
            // Prepare resource for testing.
            var appData = ApplicationData.Current;
            var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(LOCAL_PNG_URL)
                .AsTask()
                .ConfigureAwait(false);

            await sourceFile.CopyAsync(appData.LocalFolder).AsTask().ConfigureAwait(false);

            var bitmap = await _imagePipeline.FetchDecodedBitmapImageAsync(
                ImageRequest.FromUri(LOCAL_APP_DATA_URL)).ConfigureAwait(false);

            await DispatcherHelpers.RunOnDispatcherAsync(() =>
            {
                Assert.IsTrue(bitmap.PixelWidth != 0);
                Assert.IsTrue(bitmap.PixelHeight != 0);
            });
        }

        /// <summary>
        /// Tests out fetching a bitmap from roaming app data folder
        /// </summary>
        [TestMethod]
        public async Task TestFetchRoamingAppData()
        {
            // Prepare resource for testing.
            var appData = ApplicationData.Current;
            var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(LOCAL_PNG_URL)
                .AsTask()
                .ConfigureAwait(false);

            await sourceFile.CopyAsync(appData.RoamingFolder).AsTask().ConfigureAwait(false);

            var bitmap = await _imagePipeline.FetchDecodedBitmapImageAsync(
                ImageRequest.FromUri(ROAMING_APP_DATA_URL)).ConfigureAwait(false);

            await DispatcherHelpers.RunOnDispatcherAsync(() =>
            {
                Assert.IsTrue(bitmap.PixelWidth != 0);
                Assert.IsTrue(bitmap.PixelHeight != 0);
            });
        }

        /// <summary>
        /// Tests out fetching a bitmap from temp app data folder
        /// </summary>
        [TestMethod]
        public async Task TestFetchTempAppData()
        {
            // Prepare resource for testing.
            var appData = ApplicationData.Current;
            var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(LOCAL_PNG_URL)
                .AsTask()
                .ConfigureAwait(false);

            await sourceFile.CopyAsync(appData.TemporaryFolder).AsTask().ConfigureAwait(false);

            var bitmap = await _imagePipeline.FetchDecodedBitmapImageAsync(
                ImageRequest.FromUri(TEMP_APP_DATA_URL)).ConfigureAwait(false);

            await DispatcherHelpers.RunOnDispatcherAsync(() =>
            {
                Assert.IsTrue(bitmap.PixelWidth != 0);
                Assert.IsTrue(bitmap.PixelHeight != 0);
            });
        }

        /// <summary>
        /// Tests out fetching an encoded base64 image.
        /// </summary>
        [TestMethod]
        public void TestFetchEncodedBase64Image()
        {
            var data = "data:image/jpeg;base64,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";
            var uri = new Uri(data);
            var completion = new ManualResetEvent(false);
            var dataSource = _imagePipeline.FetchEncodedImage(ImageRequest.FromUri(uri), null);
            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    CloseableReference<IPooledByteBuffer> reference = response.GetResult();
                    if (reference != null)
                    {
                        IPooledByteBuffer inputStream = reference.Get();

                        try
                        {
                            Assert.IsTrue(inputStream.Size != 0);
                            Assert.IsTrue(_imagePipeline.IsInEncodedMemoryCache(uri));
                        }
                        catch (Exception)
                        {
                            Assert.Fail();
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                            completion.Set();
                        }

                        return Task.CompletedTask;
                    }
                    else
                    {
                        Assert.Fail();
                        completion.Set();
                        return Task.CompletedTask;
                    }
                },
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching an encoded png file from local assets
        /// </summary>
        [TestMethod]
        public void TestFetchEncodedLocalPng()
        {
            var completion = new ManualResetEvent(false);
            var dataSource = _imagePipeline.FetchEncodedImage(
                ImageRequest.FromUri(LOCAL_PNG_URL), null);

            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    CloseableReference<IPooledByteBuffer> reference = response.GetResult();
                    if (reference != null)
                    {
                        IPooledByteBuffer inputStream = reference.Get();

                        try
                        {
                            Assert.IsTrue(inputStream.Size != 0);
                            Assert.IsTrue(_imagePipeline.IsInEncodedMemoryCache(LOCAL_PNG_URL));
                        }
                        catch (Exception)
                        {
                            Assert.Fail();
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                            completion.Set();
                        }

                        return Task.CompletedTask;
                    }
                    else
                    {
                        Assert.Fail();
                        completion.Set();
                        return Task.CompletedTask;
                    }
                },
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching an encoded jpeg file from local assets
        /// </summary>
        [TestMethod]
        public void TestFetchEncodedLocalJpeg()
        {
            var completion = new ManualResetEvent(false);
            var dataSource = _imagePipeline.FetchEncodedImage(
                ImageRequest.FromUri(LOCAL_JPEG_URL), null);

            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    CloseableReference<IPooledByteBuffer> reference = response.GetResult();
                    if (reference != null)
                    {
                        IPooledByteBuffer inputStream = reference.Get();

                        try
                        {
                            Assert.IsTrue(inputStream.Size != 0);
                            Assert.IsTrue(_imagePipeline.IsInEncodedMemoryCache(LOCAL_JPEG_URL));
                        }
                        catch (Exception)
                        {
                            Assert.Fail();
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                            completion.Set();
                        }

                        return Task.CompletedTask;
                    }
                    else
                    {
                        Assert.Fail();
                        completion.Set();
                        return Task.CompletedTask;
                    }
                },
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching an encoded gif file from local assets
        /// </summary>
        [TestMethod]
        public void TestFetchEncodedLocalGif()
        {
            var completion = new ManualResetEvent(false);
            var dataSource = _imagePipeline.FetchEncodedImage(
                ImageRequest.FromUri(LOCAL_GIF_URL), null);

            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    CloseableReference<IPooledByteBuffer> reference = response.GetResult();
                    if (reference != null)
                    {
                        IPooledByteBuffer inputStream = reference.Get();

                        try
                        {
                            Assert.IsTrue(inputStream.Size != 0);
                            Assert.IsTrue(_imagePipeline.IsInEncodedMemoryCache(LOCAL_GIF_URL));
                        }
                        catch (Exception)
                        {
                            Assert.Fail();
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                            completion.Set();
                        }

                        return Task.CompletedTask;
                    }
                    else
                    {
                        Assert.Fail();
                        completion.Set();
                        return Task.CompletedTask;
                    }
                },
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching an encoded image using file URI
        /// </summary>
        [TestMethod]
        public async Task TestFetchEncodedLocalFileUri()
        {
            // Prepare resource for testing.
            var appData = ApplicationData.Current;
            var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(LOCAL_PNG4_URL)
                .AsTask()
                .ConfigureAwait(false);

            await sourceFile.CopyAsync(appData.LocalFolder).AsTask().ConfigureAwait(false);

            var fileUri = new Uri($"file:///{ appData.LocalFolder.Path }/4.png");
            var completion = new ManualResetEvent(false);
            var dataSource = _imagePipeline.FetchEncodedImage(ImageRequest.FromUri(fileUri), null);
            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    CloseableReference<IPooledByteBuffer> reference = response.GetResult();
                    if (reference != null)
                    {
                        IPooledByteBuffer inputStream = reference.Get();

                        try
                        {
                            Assert.IsTrue(inputStream.Size != 0);
                            Assert.IsTrue(_imagePipeline.IsInEncodedMemoryCache(fileUri));
                        }
                        catch (Exception)
                        {
                            Assert.Fail();
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                            completion.Set();
                        }

                        return Task.CompletedTask;
                    }
                    else
                    {
                        Assert.Fail();
                        completion.Set();
                        return Task.CompletedTask;
                    }
                },
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching an encoded image from local app data folder
        /// </summary>
        [TestMethod]
        public async Task TestFetchEncodedImageLocalAppData()
        {
            // Prepare resource for testing.
            var appData = ApplicationData.Current;
            var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(LOCAL_PNG2_URL)
                .AsTask()
                .ConfigureAwait(false);

            await sourceFile.CopyAsync(appData.LocalFolder).AsTask().ConfigureAwait(false);

            var completion = new ManualResetEvent(false);
            var dataSource = _imagePipeline.FetchEncodedImage(
                ImageRequest.FromUri(LOCAL_APP_DATA2_URL), null);

            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    CloseableReference<IPooledByteBuffer> reference = response.GetResult();
                    if (reference != null)
                    {
                        IPooledByteBuffer inputStream = reference.Get();

                        try
                        {
                            Assert.IsTrue(inputStream.Size != 0);
                            Assert.IsTrue(_imagePipeline.IsInEncodedMemoryCache(
                                LOCAL_APP_DATA_URL));
                        }
                        catch (Exception)
                        {
                            Assert.Fail();
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                            completion.Set();
                        }

                        return Task.CompletedTask;
                    }
                    else
                    {
                        Assert.Fail();
                        completion.Set();
                        return Task.CompletedTask;
                    }
                },
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching an encoded image from roaming app data folder
        /// </summary>
        [TestMethod]
        public async Task TestFetchEncodedImageRoamingAppData()
        {
            // Prepare resource for testing.
            var appData = ApplicationData.Current;
            var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(LOCAL_PNG2_URL)
                .AsTask()
                .ConfigureAwait(false);

            await sourceFile.CopyAsync(appData.RoamingFolder).AsTask().ConfigureAwait(false);

            var completion = new ManualResetEvent(false);
            var dataSource = _imagePipeline.FetchEncodedImage(
                ImageRequest.FromUri(ROAMING_APP_DATA2_URL), null);

            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    CloseableReference<IPooledByteBuffer> reference = response.GetResult();
                    if (reference != null)
                    {
                        IPooledByteBuffer inputStream = reference.Get();

                        try
                        {
                            Assert.IsTrue(inputStream.Size != 0);
                            Assert.IsTrue(_imagePipeline.IsInEncodedMemoryCache(
                                ROAMING_APP_DATA_URL));
                        }
                        catch (Exception)
                        {
                            Assert.Fail();
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                            completion.Set();
                        }

                        return Task.CompletedTask;
                    }
                    else
                    {
                        Assert.Fail();
                        completion.Set();
                        return Task.CompletedTask;
                    }
                },
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching an encoded image from temp app data folder
        /// </summary>
        [TestMethod]
        public async Task TestFetchEncodedImageTempAppData()
        {
            // Prepare resource for testing.
            var appData = ApplicationData.Current;
            var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(LOCAL_PNG2_URL)
                .AsTask()
                .ConfigureAwait(false);

            await sourceFile.CopyAsync(appData.TemporaryFolder).AsTask().ConfigureAwait(false);

            var completion = new ManualResetEvent(false);
            var dataSource = _imagePipeline.FetchEncodedImage(
                ImageRequest.FromUri(TEMP_APP_DATA_URL), null);

            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    CloseableReference<IPooledByteBuffer> reference = response.GetResult();
                    if (reference != null)
                    {
                        IPooledByteBuffer inputStream = reference.Get();

                        try
                        {
                            Assert.IsTrue(inputStream.Size != 0);
                            Assert.IsTrue(_imagePipeline.IsInEncodedMemoryCache(
                                TEMP_APP_DATA_URL));
                        }
                        catch (Exception)
                        {
                            Assert.Fail();
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                            completion.Set();
                        }

                        return Task.CompletedTask;
                    }
                    else
                    {
                        Assert.Fail();
                        completion.Set();
                        return Task.CompletedTask;
                    }
                },
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching a jpeg file with Exif info from local assets
        /// </summary>
        [TestMethod]
        public async Task TestFetchLocalJpegExif()
        {
            var imageRequest = ImageRequestBuilder
                .NewBuilderWithSource(LOCAL_JPEG_EXIF_URL)
                .SetAutoRotateEnabled(true)
                .Build();

            var bitmap = await _imagePipeline.FetchDecodedBitmapImageAsync(imageRequest)
                .ConfigureAwait(false);

            await DispatcherHelpers.RunOnDispatcherAsync(() =>
            {
                Assert.IsTrue(bitmap.PixelWidth != 0);
                Assert.IsTrue(bitmap.PixelHeight != 0);
            });
        }

        ///// <summary>
        ///// Tests out fetching a jpeg file and resize.
        ///// </summary>
        //[TestMethod]
        //public async Task TestFetchLocalJpegResize()
        //{
        //    var imageRequest = ImageRequestBuilder
        //        .NewBuilderWithSource(LOCAL_JPEG_URL)
        //        .SetResizeOptions(new ResizeOptions(120, 91))
        //        .Build();

        //    var bitmap = await _imagePipeline.FetchDecodedBitmapImageAsync(imageRequest)
        //        .ConfigureAwait(false);

        //    await DispatcherHelpers.RunOnDispatcherAsync(() =>
        //    {
        //        Assert.IsTrue(bitmap.PixelWidth == 120);
        //        Assert.IsTrue(bitmap.PixelHeight == 91);
        //    });
        //}

        /// <summary>
        /// Tests out fetching an encoded image from future access list
        /// </summary>
        [TestMethod]
        public async Task TestFetchEncodedImageFutureAccessList()
        {
            // Prepare resource for testing.
            var fa = StorageApplicationPermissions.FutureAccessList;
            var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(LOCAL_PNG_URL)
                .AsTask()
                .ConfigureAwait(false);

            string token = fa.Add(sourceFile, "test");

            var completion = new ManualResetEvent(false);
            var uri = new Uri($"urn:future-access-list:{ token }");
            var dataSource = _imagePipeline.FetchEncodedImage(
                ImageRequest.FromUri(uri), null);

            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    CloseableReference<IPooledByteBuffer> reference = response.GetResult();
                    if (reference != null)
                    {
                        IPooledByteBuffer inputStream = reference.Get();

                        try
                        {
                            Assert.IsTrue(inputStream.Size != 0);
                            Assert.IsTrue(_imagePipeline.IsInEncodedMemoryCache(uri));
                        }
                        catch (Exception)
                        {
                            Assert.Fail();
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                            completion.Set();
                        }

                        return Task.CompletedTask;
                    }
                    else
                    {
                        Assert.Fail();
                        completion.Set();
                        return Task.CompletedTask;
                    }
                },
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching an invalid encoded image from future access list
        /// </summary>
        [TestMethod]
        public void TestFetchEncodedImageFutureAccessListFail()
        {
            var completion = new ManualResetEvent(false);
            var uri = new Uri($"urn:future-access-list:{ INVALID_TOKEN }");
            var dataSource = _imagePipeline.FetchEncodedImage(
                ImageRequest.FromUri(uri), null);

            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                response =>
                {
                    Assert.Fail();
                    completion.Set();
                    return Task.CompletedTask;
                },
                response =>
                {
                    completion.Set();
                });

            dataSource.Subscribe(dataSubscriber, CallerThreadExecutor.Instance);
            completion.WaitOne();
        }

        /// <summary>
        /// Tests out fetching a decoded image from future access list
        /// </summary>
        [TestMethod]
        public async Task TestFetchFutureAccessList()
        {
            // Prepare resource for testing.
            var fa = StorageApplicationPermissions.FutureAccessList;
            var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(LOCAL_PNG_URL)
                .AsTask()
                .ConfigureAwait(false);

            string token = fa.Add(sourceFile, "test");

            var uri = new Uri($"urn:future-access-list:{ token }");
            var bitmap = await _imagePipeline.FetchDecodedBitmapImageAsync(
                ImageRequest.FromUri(uri)).ConfigureAwait(false);

            await DispatcherHelpers.RunOnDispatcherAsync(() =>
            {
                Assert.IsTrue(bitmap.PixelWidth != 0);
                Assert.IsTrue(bitmap.PixelHeight != 0);
            });
        }

        /// <summary>
        /// Tests out fetching a decoded image from future access list.
        /// The uri is encoded.
        /// </summary>
        [TestMethod]
        public async Task TestFetchFutureAccessListEncodedURI()
        {
            // Prepare resource for testing.
            var fa = StorageApplicationPermissions.FutureAccessList;
            var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(LOCAL_PNG_URL)
                .AsTask()
                .ConfigureAwait(false);

            string token = WebUtility.UrlEncode(fa.Add(sourceFile, "test"));

            var uri = new Uri($"urn:future-access-list:{ token }");
            var bitmap = await _imagePipeline.FetchDecodedBitmapImageAsync(
                ImageRequest.FromUri(uri)).ConfigureAwait(false);

            await DispatcherHelpers.RunOnDispatcherAsync(() =>
            {
                Assert.IsTrue(bitmap.PixelWidth != 0);
                Assert.IsTrue(bitmap.PixelHeight != 0);
            });
        }

        /// <summary>
        /// Tests out getting the file cache path when the file has been
        /// written to disk successfully.
        /// </summary>
        [TestMethod, Timeout(5000)]
        public async Task TestGetFileCachePathSuccess()
        {
            await _imagePipeline.PrefetchToDiskCacheAsync(IMAGE_URL).ConfigureAwait(false);
            Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE_URL).ConfigureAwait(false));

            FileInfo info = await _imagePipeline.GetFileCachePath(IMAGE_URL).ConfigureAwait(false);
            Assert.IsNotNull(info);
        }

        /// <summary>
        /// Tests out getting the file cache path when the file doesn't exist.
        /// </summary>
        [TestMethod, Timeout(5000)]
        public async Task TestGetFileCachePathFail()
        {
            await _imagePipeline.PrefetchToDiskCacheAsync(IMAGE_URL).ConfigureAwait(false);
            Assert.IsTrue(await _imagePipeline.IsInDiskCacheAsync(IMAGE_URL).ConfigureAwait(false));

            FileInfo info = await _imagePipeline.GetFileCachePath(FAILURE_URL).ConfigureAwait(false);
            Assert.IsNull(info);
        }
    }
}
