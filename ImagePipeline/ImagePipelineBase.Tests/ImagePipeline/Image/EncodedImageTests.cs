using FBCore.Common.Internal;
using FBCore.Common.References;
using ImageFormatUtils;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipelineBase.Tests.ImagePipeline.Cache;
using ImageUtils;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace ImagePipelineBase.Tests.ImagePipeline.Image
{
    /// <summary>
    /// Basic tests for <see cref="EncodedImage"/> 
    /// </summary>
    [TestClass]
    public class EncodedImageTests
    {
        private const int ENCODED_BYTES_LENGTH = 100;

        private CloseableReference<IPooledByteBuffer> _byteBufferRef;
        private FileStream _inputStream = default(FileStream);
        private ISupplier<FileStream> _inputStreamSupplier;
        private static readonly ResourceReleaserHelper<IPooledByteBuffer> _releaser =
            new ResourceReleaserHelper<IPooledByteBuffer>(
                b =>
                {
                    b.Dispose();
                });

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _byteBufferRef = CloseableReference<IPooledByteBuffer>.of(
                new TrivialPooledByteBuffer(new byte[] { }), _releaser);
            _inputStreamSupplier = new MockSupplier<FileStream>(_inputStream);
        }

        /// <summary>
        /// Tests the byte buffer ref
        /// </summary>
        [TestMethod]
        public void TestByteBufferRef()
        {
            EncodedImage encodedImage = new EncodedImage(_byteBufferRef);
            Assert.AreEqual(2, _byteBufferRef.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            Assert.AreSame(
                encodedImage.GetByteBufferRef().GetUnderlyingReferenceTestOnly(),
                _byteBufferRef.GetUnderlyingReferenceTestOnly());
        }

        /// <summary>
        /// Tests out the GetInputStream method
        /// </summary>
        [TestMethod]
        public void TestInputStream()
        {
            EncodedImage encodedImage = new EncodedImage(_inputStreamSupplier);
            Assert.AreSame(encodedImage.GetInputStream(), _inputStreamSupplier.Get());
        }

        /// <summary>
        /// Tests out the CloneOrNull method
        /// </summary>
        [TestMethod]
        public void TestCloneOrNull()
        {
            EncodedImage encodedImage = new EncodedImage(_byteBufferRef);
            encodedImage.Format = ImageFormat.JPEG;
            encodedImage.RotationAngle = 0;
            encodedImage.Width = 1;
            encodedImage.Height = 2;
            encodedImage.SampleSize = 4;
            EncodedImage encodedImage2 = EncodedImage.CloneOrNull(encodedImage);
            Assert.AreEqual(3, _byteBufferRef.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            Assert.AreSame(
                encodedImage.GetByteBufferRef().GetUnderlyingReferenceTestOnly(),
                encodedImage2.GetByteBufferRef().GetUnderlyingReferenceTestOnly());
            Assert.AreEqual(encodedImage.Format, encodedImage2.Format);
            Assert.AreEqual(encodedImage.RotationAngle, encodedImage2.RotationAngle);
            Assert.AreEqual(encodedImage.Height, encodedImage2.Height);
            Assert.AreEqual(encodedImage.Width, encodedImage2.Width);
            Assert.AreEqual(encodedImage.SampleSize, encodedImage2.SampleSize);

            encodedImage = new EncodedImage(_inputStreamSupplier, 100);
            encodedImage.Format = ImageFormat.JPEG;
            encodedImage.RotationAngle = 0;
            encodedImage.Width = 1;
            encodedImage.Height = 2;
            encodedImage2 = EncodedImage.CloneOrNull(encodedImage);
            Assert.AreSame(encodedImage.GetInputStream(), encodedImage2.GetInputStream());
            Assert.AreEqual(encodedImage2.Size, encodedImage.Size);
        }

        /// <summary>
        /// Tests out the CloneOrNull method
        /// </summary>
        [TestMethod]
        public void TestCloneOrNull_WithInvalidOrNullReferences()
        {
            Assert.AreEqual(null, EncodedImage.CloneOrNull(null));
            EncodedImage encodedImage = new EncodedImage(_byteBufferRef);

            encodedImage.Dispose();
            Assert.AreEqual(null, EncodedImage.CloneOrNull(encodedImage));
        }

        /// <summary>
        /// Tests out the Dispose method
        /// </summary>
        [TestMethod]
        public void TestClose()
        {
            EncodedImage encodedImage = new EncodedImage(_byteBufferRef);
            encodedImage.Dispose();
            Assert.AreEqual(1, _byteBufferRef.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
        }

        /// <summary>
        /// Tests out the IsValid method
        /// </summary>
        [TestMethod]
        public void TestIsValid()
        {
            EncodedImage encodedImage = new EncodedImage(_byteBufferRef);
            Assert.IsTrue(encodedImage.IsValid());
            encodedImage.Dispose();
            Assert.IsFalse(encodedImage.IsValid());
            encodedImage = new EncodedImage(_inputStreamSupplier);
            Assert.IsTrue(encodedImage.IsValid());

            // Test the static method
            Assert.IsFalse(EncodedImage.IsValid(null));
        }

        /// <summary>
        /// Tests out the IsMetaDataAvailable method
        /// </summary>
        [TestMethod]
        public void TestIsMetaDataAvailable()
        {
            EncodedImage encodedImage1 = new EncodedImage(_byteBufferRef);
            EncodedImage encodedImage2 = new EncodedImage(_byteBufferRef);
            encodedImage2.RotationAngle = 1;
            encodedImage2.Width = 1;
            encodedImage2.Height = 1;
            Assert.IsFalse(EncodedImage.IsMetaDataAvailable(encodedImage1));
            Assert.IsTrue(EncodedImage.IsMetaDataAvailable(encodedImage2));
        }

        /// <summary>
        /// Tests out the CloseSafely method
        /// </summary>
        [TestMethod]
        public void TestCloseSafely()
        {
            EncodedImage encodedImage = new EncodedImage(_byteBufferRef);
            EncodedImage.CloseSafely(encodedImage);
            Assert.AreEqual(1, _byteBufferRef.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
        }

        /// <summary>
        /// Tests out the GetInputStream method
        /// </summary>
        [TestMethod]
        public void TestGetInputStream()
        {
            EncodedImage encodedImage = new EncodedImage(_inputStreamSupplier);
            Assert.AreSame(_inputStream, encodedImage.GetInputStream());
        }

        /// <summary>
        /// Tests out the ParseMetaData method
        /// </summary>
        [TestMethod]
        public async Task TestParseMetaData_JPEG()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImagePipeline/Images/image.jpg"));
            using (var stream = await file.OpenReadAsync())
            {
                IPooledByteBuffer buf = new TrivialPooledByteBuffer(
                    ByteStreams.ToByteArray(stream.AsStream()));
                EncodedImage encodedImage = new EncodedImage(
                    CloseableReference<IPooledByteBuffer>.of(buf, _releaser));
                await encodedImage.ParseMetaDataAsync();
                Assert.AreEqual(ImageFormat.JPEG, encodedImage.Format);
                Assert.AreEqual(550, encodedImage.Width);
                Assert.AreEqual(468, encodedImage.Height);
            }
        }

        /// <summary>
        /// Tests out the ParseMetaData method
        /// </summary>
        [TestMethod]
        public async Task TestParseMetaData_PNG()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/ImagePipeline/Images/image.png"));
            using (var stream = await file.OpenReadAsync())
            {
                IPooledByteBuffer buf = new TrivialPooledByteBuffer(
                    ByteStreams.ToByteArray(stream.AsStream()));
                EncodedImage encodedImage = new EncodedImage(
                    CloseableReference<IPooledByteBuffer>.of(buf, _releaser));
                await encodedImage.ParseMetaDataAsync();
                Assert.AreEqual(ImageFormat.PNG, encodedImage.Format);
                Assert.AreEqual(800, encodedImage.Width);
                Assert.AreEqual(600, encodedImage.Height);
            }
        }

        /// <summary>
        /// Tests out the IsCompleteAt method
        /// </summary>
        [TestMethod]
        public void TestIsJpegCompleteAt_NotComplete()
        {
            byte[] encodedBytes = new byte[ENCODED_BYTES_LENGTH];
            encodedBytes[ENCODED_BYTES_LENGTH - 2] = 0;
            encodedBytes[ENCODED_BYTES_LENGTH - 1] = 0;
            IPooledByteBuffer buf = new TrivialPooledByteBuffer(encodedBytes);
            EncodedImage encodedImage = new EncodedImage(
                CloseableReference<IPooledByteBuffer>.of(buf, _releaser));
            encodedImage.Format = ImageFormat.JPEG;
            Assert.IsFalse(encodedImage.IsCompleteAt(ENCODED_BYTES_LENGTH));
        }

        /// <summary>
        /// Tests out the IsCompleteAt method
        /// </summary>
        [TestMethod]
        public void TestIsJpegCompleteAt_Complete()
        {
            byte[] encodedBytes = new byte[ENCODED_BYTES_LENGTH];
            encodedBytes[ENCODED_BYTES_LENGTH - 2] = JfifUtil.MARKER_FIRST_BYTE;
            encodedBytes[ENCODED_BYTES_LENGTH - 1] = JfifUtil.MARKER_EOI;
            IPooledByteBuffer buf = new TrivialPooledByteBuffer(encodedBytes);
            EncodedImage encodedImage = new EncodedImage(
                CloseableReference<IPooledByteBuffer>.of(buf, _releaser));
            encodedImage.Format = ImageFormat.JPEG;
            Assert.IsTrue(encodedImage.IsCompleteAt(ENCODED_BYTES_LENGTH));
        }

        /// <summary>
        /// Tests out the CopyMetaData method
        /// </summary>
        [TestMethod]
        public void TestCopyMetaData()
        {
            EncodedImage encodedImage = new EncodedImage(_byteBufferRef);
            encodedImage.Format = ImageFormat.JPEG;
            encodedImage.RotationAngle = 0;
            encodedImage.Width = 1;
            encodedImage.Height = 2;
            encodedImage.SampleSize = 3;
            EncodedImage encodedImage2 = new EncodedImage(_byteBufferRef);
            encodedImage2.CopyMetaDataFrom(encodedImage);
            Assert.AreEqual(encodedImage.Format, encodedImage2.Format);
            Assert.AreEqual(encodedImage.Width, encodedImage2.Width);
            Assert.AreEqual(encodedImage.Height, encodedImage2.Height);
            Assert.AreEqual(encodedImage.SampleSize, encodedImage2.SampleSize);
            Assert.AreEqual(encodedImage.Size, encodedImage2.Size);

            EncodedImage encodedImage3 = new EncodedImage(_inputStreamSupplier);
            encodedImage3.Format = ImageFormat.JPEG;
            encodedImage3.RotationAngle = 0;
            encodedImage3.Width = 1;
            encodedImage3.Height = 2;
            encodedImage3.SampleSize = 3;
            encodedImage3.StreamSize = 4;
            EncodedImage encodedImage4 = new EncodedImage(_inputStreamSupplier);
            encodedImage4.CopyMetaDataFrom(encodedImage3);
            Assert.AreEqual(encodedImage3.Format, encodedImage4.Format);
            Assert.AreEqual(encodedImage3.Width, encodedImage4.Width);
            Assert.AreEqual(encodedImage3.Height, encodedImage4.Height);
            Assert.AreEqual(encodedImage3.SampleSize, encodedImage4.SampleSize);
            Assert.AreEqual(encodedImage3.Size, encodedImage4.Size);
        }
    }
}
