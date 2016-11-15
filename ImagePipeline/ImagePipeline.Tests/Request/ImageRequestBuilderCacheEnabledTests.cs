using ImagePipeline.Request;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace ImagePipeline.Tests.Request
{
    /// <summary>
    /// Tests for <see cref="ImageRequest"/>
    /// </summary>
    [TestClass]
    public class ImageRequestBuilderCacheEnabledTests
    {
        /// <summary>
        /// Tests disk cache enabled
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheEnabledByDefault1()
        {
            ImageRequestBuilder builder = 
                ImageRequestBuilder.NewBuilderWithSource(new Uri("http://request"));
            Assert.IsTrue(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache disabled on request
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheDisabledIfRequested1()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("http://request"));
            builder.DisableDiskCache();
            Assert.IsFalse(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache enabled
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheEnabledByDefault2()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("https://request"));
            Assert.IsTrue(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache disabled on request
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheDisabledIfRequested2()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("https://request"));
            builder.DisableDiskCache();
            Assert.IsFalse(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache enabled
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheEnabledByDefault3()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("ms-appx:///request"));
            Assert.IsFalse(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache disabled on request
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheDisabledIfRequested3()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("ms-appx:///request"));
            builder.DisableDiskCache();
            Assert.IsFalse(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache enabled
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheEnabledByDefault4()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("ms-appx-web:///request"));
            Assert.IsFalse(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache disabled on request
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheDisabledIfRequested4()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("ms-appx-web:///request"));
            builder.DisableDiskCache();
            Assert.IsFalse(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache enabled
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheEnabledByDefault5()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("ms-appdata:///request"));
            Assert.IsFalse(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache disabled on request
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheDisabledIfRequested5()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("ms-appdata:///request"));
            builder.DisableDiskCache();
            Assert.IsFalse(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache enabled
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheEnabledByDefault6()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("ms-resource:///request"));
            Assert.IsFalse(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache disabled on request
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheDisabledIfRequested6()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("ms-resource:///request"));
            builder.DisableDiskCache();
            Assert.IsFalse(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache enabled
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheEnabledByDefault7()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("data:image/png;base64"));
            Assert.IsFalse(builder.DiskCacheEnabled);
        }

        /// <summary>
        /// Tests disk cache disabled on request
        /// </summary>
        [TestMethod]
        public void TestIsDiskCacheDisabledIfRequested7()
        {
            ImageRequestBuilder builder =
                ImageRequestBuilder.NewBuilderWithSource(new Uri("data:image/png;base64"));
            builder.DisableDiskCache();
            Assert.IsFalse(builder.DiskCacheEnabled);
        }
    }
}
