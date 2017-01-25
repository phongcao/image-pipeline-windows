using ImageUtils;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace ImagePipelineBase.Tests.ImageUtils
{
    /// <summary>
    /// Tests for BitmapUtil
    /// </summary>
    [TestClass]
    public class BitmapUtilTests
    {
        /// <summary>
        /// Tests out the GetSizeInBytes method
        /// </summary>
        [TestMethod]
        public async Task TestGetSizeInBytes()
        {
            // 0 for null
            Assert.AreEqual(0, (int)BitmapUtil.GetSizeInBytes(null));

            // 240 * 181 * 4 = 173760
            var file1 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageFormatUtils/pngs/1.png"));
            using (var stream = await file1.OpenReadAsync())
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                using (SoftwareBitmap bitmap = await decoder.GetSoftwareBitmapAsync())
                {
                    Assert.AreEqual(173760, (int)BitmapUtil.GetSizeInBytes(bitmap));
                }
            }

            // 240 * 246 * 4 = 236160
            var file2 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageFormatUtils/pngs/2.png"));
            using (var stream = await file2.OpenReadAsync())
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                using (SoftwareBitmap bitmap = await decoder.GetSoftwareBitmapAsync())
                {
                    Assert.AreEqual(236160, (int)BitmapUtil.GetSizeInBytes(bitmap));
                }
            }

            // 240 * 180 * 4 = 172800
            var file3 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageFormatUtils/pngs/3.png"));
            using (var stream = await file3.OpenReadAsync())
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                using (SoftwareBitmap bitmap = await decoder.GetSoftwareBitmapAsync())
                {
                    Assert.AreEqual(172800, (int)BitmapUtil.GetSizeInBytes(bitmap));
                }
            }
        }

        /// <summary>
        /// Tests out the DecodeDimensions method
        /// </summary>
        [TestMethod]
        public async Task TestDecodeDimensions_TestPngs()
        {
            var file1 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/pngs/1.png"));
            using (var stream = await file1.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 181), dimensions);
            }

            var file2 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/pngs/2.png"));
            using (var stream = await file2.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 246), dimensions);
            }

            var file3 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/pngs/3.png"));
            using (var stream = await file3.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 180), dimensions);
            }
        }

        /// <summary>
        /// Tests out the DecodeDimensions method
        /// </summary>
        [TestMethod]
        public async Task TestDecodeDimensions_TestJpegs()
        {
            var file1 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/jpegs/1.jpeg"));
            using (var stream = await file1.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 181), dimensions);
            }

            var file2 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/jpegs/2.jpeg"));
            using (var stream = await file2.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 93), dimensions);
            }

            var file3 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/jpegs/3.jpeg"));
            using (var stream = await file3.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 240), dimensions);
            }
        }

        /// <summary>
        /// Tests out the DecodeDimensions method
        /// </summary>
        [TestMethod]
        public async Task TestDecodeDimensions_TestIncompleteJpegs()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/jpegs/1cut.jpeg"));
            using (var stream = await file.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 181), dimensions);
            }
        }

        /// <summary>
        /// Tests out the DecodeDimensions method
        /// </summary>
        [TestMethod]
        public async Task TestDecodeDimensions_TestProgressiveJpegs()
        {
            var file1 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/jpegs/1prog.jpeg"));
            using (var stream = await file1.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(981, 657), dimensions);
            }

            var file2 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/jpegs/2prog.jpeg"));
            using (var stream = await file2.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(800, 531), dimensions);
            }
        }

        /// <summary>
        /// Tests out the DecodeDimensions method
        /// </summary>
        [TestMethod]
        public async Task TestDecodeDimensions_TestStaticGifs()
        {
            var file1 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/gifs/1.gif"));
            using (var stream = await file1.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 181), dimensions);
            }

            var file2 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/gifs/2.gif"));
            using (var stream = await file2.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 246), dimensions);
            }

            var file3 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/gifs/3.gif"));
            using (var stream = await file3.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 180), dimensions);
            }
        }

        /// <summary>
        /// Tests out the DecodeDimensions method
        /// </summary>
        [TestMethod]
        public async Task TestDecodeDimensions_TestAnimatedGifs()
        {
            var file1 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/animatedgifs/1.gif"));
            using (var stream = await file1.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(500, 500), dimensions);
            }

            var file2 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/animatedgifs/2.gif"));
            using (var stream = await file2.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(550, 400), dimensions);
            }
        }

        /// <summary>
        /// Tests out the DecodeDimensions method
        /// </summary>
        [TestMethod]
        public async Task TestDecodeDimensions_TestBmps()
        {
            var file1 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/bmps/1.bmp"));
            using (var stream = await file1.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 181), dimensions);
            }

            var file2 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/bmps/2.bmp"));
            using (var stream = await file2.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 246), dimensions);
            }

            var file3 = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImageUtils/bmps/3.bmp"));
            using (var stream = await file3.OpenReadAsync())
            {
                Tuple<int, int> dimensions = await BitmapUtil.DecodeDimensionsAsync(stream.AsStream());
                Assert.AreEqual(new Tuple<int, int>(240, 180), dimensions);
            }
        }
    }
}
