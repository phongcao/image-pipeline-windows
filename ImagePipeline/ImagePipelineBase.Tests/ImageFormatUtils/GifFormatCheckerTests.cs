using ImageFormatUtils;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace ImagePipelineBase.Tests.ImageFormatUtils
{
    /// <summary>
    /// Tests for <see cref="GifFormatChecker"/> 
    /// </summary>
    [TestClass]
    public class GifFormatCheckerTests
    {
        /// <summary>
        /// Tests static gifs
        /// </summary>
        [TestMethod]
        public async Task TestStaticGifs()
        {
            await SingleAnimatedGifTest(GetNames(5, "gifs/{0}.gif"), false);
        }

        /// <summary>
        /// Tests animated gifs
        /// </summary>
        [TestMethod]
        public async Task TestAnimatedGifs()
        {
            await SingleAnimatedGifTest(GetNames(2, "animatedgifs/{0}.gif"), true);
        }

        /// <summary>
        /// Tests out the CircularBufferMatchesBytePattern method
        /// </summary>
        [TestMethod]
        public void TestCircularBufferMatchesBytePattern()
        {
            byte[] testBytes = new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04
            };

            Assert.IsTrue(GifFormatChecker.CircularBufferMatchesBytePattern(testBytes, 0, testBytes));
            for (int i = 1; i < testBytes.Length; i++)
            {
                Assert.IsFalse(GifFormatChecker.CircularBufferMatchesBytePattern(testBytes, i, testBytes));
            }

            byte[] testInnerBytes = new byte[] 
            {
                0x01, 0x02
            };

            Assert.IsTrue(GifFormatChecker.CircularBufferMatchesBytePattern(testBytes, 1, testInnerBytes));
            for (int i = 2; i < testBytes.Length; i++)
            {
                Assert.IsFalse(GifFormatChecker.CircularBufferMatchesBytePattern(
                    testBytes, i, testInnerBytes));
            }

            byte[] testCircularBytes = new byte[] { (byte)0x04, (byte)0x00 };
            Assert.IsTrue(GifFormatChecker.CircularBufferMatchesBytePattern(
                testBytes, 4, testCircularBytes));
            for (int i = 0; i < 4; i++)
            {
                Assert.IsFalse(GifFormatChecker.CircularBufferMatchesBytePattern(
                        testBytes, i, testCircularBytes));
            }
        }

        private async Task SingleAnimatedGifTest(
            IList<string> resourceNames,
            bool expectedAnimated)
        {
            foreach (string name in resourceNames)
            {
                Stream resourceStream = await GetResourceStream(name);
                try
                {
                    Assert.AreEqual(
                        expectedAnimated,
                        GifFormatChecker.IsAnimated(resourceStream),
                        "failed with resource: " + name);
                }
                finally
                {
                    resourceStream.Dispose();
                }
            }
        }

        private static IList<string> GetNames(int amount, string pathFormat)
        {
            IList<string> result = new List<string>();
            for (int i = 1; i <= amount; ++i)
            {
                result.Add(string.Format(pathFormat, i));
            }

            return result;
        }

        private async Task<Stream> GetResourceStream(string name)
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageFormatUtils/" + name));
            return (await file.OpenReadAsync()).AsStream();
        }
    }
}
