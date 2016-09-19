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
    /// Tests for <see cref="ImageFormatChecker"/> 
    /// </summary>
    [TestClass]
    public class ImageFormatCheckerTests
    {
        /// <summary>
        /// Tests out webps
        /// </summary>
        [TestMethod]
        public async Task TestSimpleWebps()
        {
            await SingleImageTypeTest(
                    GetNames(2, "webps/{0}_webp_plain.webp"),
                    ImageFormat.WEBP_SIMPLE);
        }

        /// <summary>
        /// Tests out webps lossless
        /// </summary>
        [TestMethod]
        public async Task TestLosslessWebps()
        {
            await SingleImageTypeTest(
                    GetNames(5, "webps/{0}_webp_ll.webp"),
                    ImageFormat.WEBP_LOSSLESS);
        }

        /// <summary>
        /// Tests out extended webps with alpha
        /// </summary>
        [TestMethod]
        public async Task TestExtendedWebpsWithAlpha()
        {
            await SingleImageTypeTest(
                    GetNames(5, "webps/{0}_webp_ea.webp"),
                    ImageFormat.WEBP_EXTENDED_WITH_ALPHA);
        }

        /// <summary>
        /// Tests out extended webps without alpha
        /// </summary>
        [TestMethod]
        public async Task TestExtendedWebpsWithoutAlpha()
        {
            await SingleImageTypeTest(
                    GetName("webps/1_webp_e.webp"),
                    ImageFormat.WEBP_EXTENDED);
        }

        /// <summary>
        /// Tests out animated webps
        /// </summary>
        [TestMethod]
        public async Task TestAnimatedWebps()
        {
            await SingleImageTypeTest(
                    GetName("webps/1_webp_anim.webp"),
                    ImageFormat.WEBP_ANIMATED);
        }

        /// <summary>
        /// Tests out jpeg
        /// </summary>
        [TestMethod]
        public async Task TestJpegs()
        {
            await SingleImageTypeTest(
                    GetNames(5, "jpegs/{0}.jpeg"),
                    ImageFormat.JPEG);
        }

        /// <summary>
        /// Tests out png
        /// </summary>
        [TestMethod]
        public async Task TestPngs()
        {
            await SingleImageTypeTest(
                    GetNames(5, "pngs/{0}.png"),
                    ImageFormat.PNG);
        }

        /// <summary>
        /// Tests out gif
        /// </summary>
        [TestMethod]
        public async Task TestGifs()
        {
            await SingleImageTypeTest(
                    GetNames(5, "gifs/{0}.gif"),
                    ImageFormat.GIF);
        }

        /// <summary>
        /// Tests out bmp
        /// </summary>
        [TestMethod]
        public async Task TestBmps()
        {
            await SingleImageTypeTest(
                GetNames(5, "bmps/{0}.bmp"),
                ImageFormat.BMP);
        }

        private async Task SingleImageTypeTest(
            IList<string> resourceNames,
            ImageFormat expectedImageType)
        {
            foreach (string name in resourceNames)
            {
                Stream resourceStream = await GetResourceStream(name);
                try
                {
                    Assert.AreEqual(
                        expectedImageType,
                        ImageFormatChecker.GetImageFormat(resourceStream),
                        "failed with resource: " + name);
                }
                finally
                {
                    resourceStream.Dispose();
                }
            }
        }

        private static IList<string> GetName(string path)
        {
            return new List<string>
            {
                path
            };
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
