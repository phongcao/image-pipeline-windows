using ImageUtils;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using static ImagePipelineBase.Tests.ImageUtils.JfifTestUtils;

namespace ImagePipelineBase.Tests.ImageUtils
{
    /// <summary>
    /// Tests for <see cref="JfifUtil"/> 
    /// </summary>
    [TestClass]
    public class JfifUtilTests
    {
        // Test cases without APP1 block
        private readonly string NO_ORI_IMAGE_1 = SOI + APP0 + APP2 + DQT + DHT + DRI + SOF + SOS + EOI;
        private readonly string NO_ORI_IMAGE_2 = SOI + DQT + DHT + SOF + SOS + EOI;

        /// <summary>
        /// Tests out the GetOrientation method
        /// </summary>
        [TestMethod]
        public void TestGetOrientation_NoAPP1()
        {
            Assert.AreEqual(0, JfifUtil.GetOrientation(HexStringToByteArray(NO_ORI_IMAGE_1)));
            Assert.AreEqual(0, JfifUtil.GetOrientation(HexStringToByteArray(NO_ORI_IMAGE_2)));
        }

        /// <summary>
        /// Tests out the GetOrientation method
        /// </summary>
        [TestMethod]
        public void TestGetOrientation_BigEndian()
        {
            TestGetOrientation_WithEndian(false);
        }

        /// <summary>
        /// Tests out the GetOrientation method
        /// </summary>
        [TestMethod]
        public void TestGetOrientation_LittleEndian()
        {
            TestGetOrientation_WithEndian(true);
        }

        private void TestGetOrientation_WithEndian(bool littleEnd)
        {
            string IFD_ENTRY_1 = MakeIfdEntry(IFD_ENTRY_TAG_1, TYPE_SHORT, 1, 255, 2, littleEnd);
            string IFD_ENTRY_2 = MakeIfdEntry(IFD_ENTRY_TAG_2, TYPE_SHORT, 1, 255, 2, littleEnd);
            string IFD_ENTRY_3 = MakeIfdEntry(IFD_ENTRY_TAG_3, TYPE_SHORT, 1, 255, 2, littleEnd);
            string TIFF_IFD_0 = MakeIfd(
                new string[] 
                {
                    IFD_ENTRY_1, IFD_ENTRY_2, IFD_ENTRY_3
                }, 
                0, 
                littleEnd);
            string TIFF_IFD_1 = MakeIfd(
                new string[] 
                {
                    MakeOrientationEntry(1, littleEnd), IFD_ENTRY_1, IFD_ENTRY_2
                },
                0,
                littleEnd);
            string TIFF_IFD_3 = MakeIfd(
                new string[] 
                {
                    MakeOrientationEntry(3, littleEnd), IFD_ENTRY_1, IFD_ENTRY_2
                },
                0,
                littleEnd);
            string TIFF_IFD_6A = MakeIfd(
                new string[] 
                {
                    MakeOrientationEntry(6, littleEnd), IFD_ENTRY_1, IFD_ENTRY_2
                },
                0,
                littleEnd);
            string TIFF_IFD_6B = MakeIfd(
                new string[] 
                {
                    IFD_ENTRY_1, MakeOrientationEntry(6, littleEnd), IFD_ENTRY_2
                },
                0,
                littleEnd);
            string TIFF_IFD_6C = MakeIfd(
                    new string[] 
                    {
                        IFD_ENTRY_1, IFD_ENTRY_2, MakeOrientationEntry(6, littleEnd)
                    },
                    0,
                    littleEnd);
            string TIFF_IFD_8 = MakeIfd(
                    new string[] 
                    {
                        MakeOrientationEntry(8, littleEnd), IFD_ENTRY_1, IFD_ENTRY_2
                    },
                    0,
                    littleEnd);

            string APP1_0 = MakeAPP1_EXIF(MakeTiff(TIFF_IFD_0, littleEnd));
            string APP1_1 = MakeAPP1_EXIF(MakeTiff(TIFF_IFD_1, littleEnd));
            string APP1_3 = MakeAPP1_EXIF(MakeTiff(TIFF_IFD_3, littleEnd));
            string APP1_6A = MakeAPP1_EXIF(MakeTiff(TIFF_IFD_6A, littleEnd));
            string APP1_6B = MakeAPP1_EXIF(MakeTiff(TIFF_IFD_6B, littleEnd));
            string APP1_6C = MakeAPP1_EXIF(MakeTiff(TIFF_IFD_6C, littleEnd));
            string APP1_8 = MakeAPP1_EXIF(MakeTiff(TIFF_IFD_8, littleEnd));

            Assert.AreEqual(0, JfifUtil.GetOrientation(
                HexStringToByteArray(MakeTestImageWithAPP1(APP1_0))));
            Assert.AreEqual(1, JfifUtil.GetOrientation(
                HexStringToByteArray(MakeTestImageWithAPP1(APP1_1))));
            Assert.AreEqual(3, JfifUtil.GetOrientation(
                HexStringToByteArray(MakeTestImageWithAPP1(APP1_3))));
            Assert.AreEqual(6, JfifUtil.GetOrientation(
                HexStringToByteArray(MakeTestImageWithAPP1(APP1_6A))));
            Assert.AreEqual(6, JfifUtil.GetOrientation(
                HexStringToByteArray(MakeTestImageWithAPP1(APP1_6B))));
            Assert.AreEqual(6, JfifUtil.GetOrientation(
                HexStringToByteArray(MakeTestImageWithAPP1(APP1_6C))));
            Assert.AreEqual(8, JfifUtil.GetOrientation(
                HexStringToByteArray(MakeTestImageWithAPP1(APP1_8))));

            TestGetOrientation_VariousAPP1Location(APP1_3, 3);
        }

        private void TestGetOrientation_VariousAPP1Location(string APP1, int expectOri)
        {
            string IMAGE_WITH_STRUCT_1 = SOI + APP1 + DQT + DHT + SOF + SOS + EOI;
            string IMAGE_WITH_STRUCT_2 = SOI + DQT + APP1 + DHT + SOF + SOS + EOI;
            string IMAGE_WITH_STRUCT_3 = SOI + DQT + DHT + APP1 + DRI + SOF + SOS + EOI;
            string IMAGE_WITH_STRUCT_4 = SOI + DQT + DHT + DRI + APP1 + SOF + SOS + EOI;
            string IMAGE_WITH_STRUCT_5 = SOI + DQT + DHT + DRI + SOF + APP1 + SOS + EOI;
            string IMAGE_WITH_STRUCT_6 = SOI + DQT + DHT + SOF + APP1 + SOS + EOI;
            string IMAGE_WITH_STRUCT_7 = SOI + APP0 + APP2 + APP1 + DQT + DHT + DRI + SOF + SOS + EOI;
            string IMAGE_WITH_STRUCT_8 = SOI + APP0 + APP1 + APP2 + DQT + DHT + DRI + SOF + SOS + EOI;
            string IMAGE_WITH_STRUCT_9 = SOI + APP1 + APP2 + DQT + DHT + DRI + SOF + SOS + EOI;
            string IMAGE_WITH_STRUCT_10 = SOI + APP1 + SOS + EOI;
            Assert.AreEqual(expectOri, JfifUtil.GetOrientation(
                HexStringToByteArray(IMAGE_WITH_STRUCT_1)));
            Assert.AreEqual(expectOri, JfifUtil.GetOrientation(
                HexStringToByteArray(IMAGE_WITH_STRUCT_2)));
            Assert.AreEqual(expectOri, JfifUtil.GetOrientation(
                HexStringToByteArray(IMAGE_WITH_STRUCT_3)));
            Assert.AreEqual(expectOri, JfifUtil.GetOrientation(
                HexStringToByteArray(IMAGE_WITH_STRUCT_4)));
            Assert.AreEqual(expectOri, JfifUtil.GetOrientation(
                HexStringToByteArray(IMAGE_WITH_STRUCT_5)));
            Assert.AreEqual(expectOri, JfifUtil.GetOrientation(
                HexStringToByteArray(IMAGE_WITH_STRUCT_6)));
            Assert.AreEqual(expectOri, JfifUtil.GetOrientation(
                HexStringToByteArray(IMAGE_WITH_STRUCT_7)));
            Assert.AreEqual(expectOri, JfifUtil.GetOrientation(
                HexStringToByteArray(IMAGE_WITH_STRUCT_8)));
            Assert.AreEqual(expectOri, JfifUtil.GetOrientation(
                HexStringToByteArray(IMAGE_WITH_STRUCT_9)));
            Assert.AreEqual(expectOri, JfifUtil.GetOrientation(
                HexStringToByteArray(IMAGE_WITH_STRUCT_10)));
        }
    }
}
