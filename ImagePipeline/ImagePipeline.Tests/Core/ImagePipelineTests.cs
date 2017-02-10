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
        }
    }
}
