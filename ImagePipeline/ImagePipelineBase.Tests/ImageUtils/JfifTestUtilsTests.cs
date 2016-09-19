using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using static ImagePipelineBase.Tests.ImageUtils.JfifTestUtils;

namespace ImagePipelineBase.Tests.ImageUtils
{
    /// <summary>
    /// Tests for <see cref="JfifTestUtils"/>
    /// </summary>
    [TestClass]
    public class JfifTestUtilsTests
    {
        private string _testStr = "0123456789";

        /// <summary>
        /// Test out the MakeSOFSection method
        /// </summary>
        [TestMethod]
        public void TestMakeSOFSection()
        {
            Assert.AreEqual("FFC0000A0300FF0136000000", MakeSOFSection(
                10, // length
                3,  // bit depth
                310,  // width
                255));  // height

            Assert.AreEqual("FFC0001401013600FF00000000000000000000000000", MakeSOFSection(
                20, // length
                1,  // bit depth
                255,  // width
                310));  // height
        }

        /// <summary>
        /// Test out the NumBytes method
        /// </summary>
        [TestMethod]
        public void TestNumBytes()
        {
            Assert.AreEqual(1, NumBytes("    3F        "));
            Assert.AreEqual(4, NumBytes("1A 2B 3C 4D"));
            Assert.AreEqual(6, NumBytes("1A2B 3C4D5E6F"));
        }

        /// <summary>
        /// Test out the HexStringToByteArray method
        /// </summary>
        [TestMethod]
        public void TestHexStringToByteArray()
        {
            CollectionAssert.AreEqual(new byte[] { 0x3F }, 
                HexStringToByteArray("    3F        "));
            CollectionAssert.AreEqual(new byte[] { 0x1A, 0x2B, 0x3C, 0x4D },
                HexStringToByteArray("1A 2B 3C 4D"));
            CollectionAssert.AreEqual(
                new byte[] { 0x1A, 0x2B, 0x3C, 0x4D, 0x5E, 0x6F },
                HexStringToByteArray("1A2B 3C4D5E6F"));
        }

        /// <summary>
        /// Test out the EncodeInt2HexString method
        /// </summary>
        [TestMethod]
        public void TestEncodeInt2HexString()
        {
            Assert.AreEqual("5B6FF432", EncodeInt2HexString(1534063666, 4, false));
            Assert.AreEqual("6FF4", EncodeInt2HexString(28660, 2, false));
            Assert.AreEqual("B6", EncodeInt2HexString(182, 1, false));
            Assert.AreEqual("32F46F5B", EncodeInt2HexString(1534063666, 4, true));
            Assert.AreEqual("F46F", EncodeInt2HexString(28660, 2, true));
            Assert.AreEqual("B6", EncodeInt2HexString(182, 1, true));
        }

        /// <summary>
        /// Test out the MakeOrientationEntry method
        /// </summary>
        [TestMethod]
        public void TestMakeOrientationEntry()
        {
            Assert.AreEqual("011200030000000100050000", MakeOrientationEntry(5, false));
            Assert.AreEqual("120103000100000005000000", MakeOrientationEntry(5, true));
        }

        /// <summary>
        /// Test out the MakeIfdEntry method
        /// </summary>
        [TestMethod]
        public void TestMakeIfdEntry()
        {
            Assert.AreEqual("011200030000000100060000", MakeIfdEntry(
                IFD_ENTRY_ORI_TAG,
                TYPE_SHORT,
                1,
                6,
                2,
                false));
            Assert.AreEqual("120103000200000003000000", MakeIfdEntry(
                IFD_ENTRY_ORI_TAG,
                TYPE_SHORT,
                2,
                3,
                2,
                true));
        }

        /// <summary>
        /// Test out the MakeIfd method
        /// </summary>
        [TestMethod]
        public void TestMakeIfd()
        {
            // Test big endian
            string IFD_ENTRY_1 = MakeIfdEntry(
                IFD_ENTRY_TAG_1, TYPE_SHORT, 1, 255, 2, false);
            string IFD_ENTRY_2 = MakeIfdEntry(
                IFD_ENTRY_TAG_2, TYPE_SHORT, 1, 255, 2, false);
            string IFD_ENTRY_3 = MakeIfdEntry(
                IFD_ENTRY_TAG_3, TYPE_SHORT, 1, 255, 2, false);
            Assert.AreEqual(
                "0003" +
                "011A00030000000100FF0000" +
                "011B00030000000100FF0000" +
                "011C00030000000100FF0000" +
                "00000008",
                MakeIfd(new string[] { IFD_ENTRY_1, IFD_ENTRY_2, IFD_ENTRY_3 }, 8, false));

            // Test little endian
            IFD_ENTRY_1 = MakeIfdEntry(
                IFD_ENTRY_TAG_1, TYPE_SHORT, 1, 255, 2, true);
            IFD_ENTRY_2 = MakeIfdEntry(
                IFD_ENTRY_TAG_2, TYPE_SHORT, 1, 255, 2, true);
            IFD_ENTRY_3 = MakeIfdEntry(
                IFD_ENTRY_TAG_3, TYPE_SHORT, 1, 255, 2, true);
            Assert.AreEqual(
                "0300" +
                "1A01030001000000FF000000" +
                "1B01030001000000FF000000" +
                "1C01030001000000FF000000" +
                "09000000",
                MakeIfd(new string[] { IFD_ENTRY_1, IFD_ENTRY_2, IFD_ENTRY_3 }, 9, true));
        }

        /// <summary>
        /// Test out the MakeTiff method
        /// </summary>
        [TestMethod]
        public void TestMakeTiff()
        {
            Assert.AreEqual(TIFF_HEADER_BE + _testStr, MakeTiff(_testStr, false));
            Assert.AreEqual(TIFF_HEADER_LE + _testStr, MakeTiff(_testStr, true));
        }

        /// <summary>
        /// Test out the MakeAPP1_EXIF method
        /// </summary>
        [TestMethod]
        public void TestMakeAPP1_EXIF()
        {
            Assert.AreEqual(APP1_MARKER + "000D" + APP1_EXIF_MAGIC + _testStr,
                MakeAPP1_EXIF(_testStr));
        }

        /// <summary>
        /// Test out the MakeTestImageWithAPP1 method
        /// </summary>
        [TestMethod]
        public void TestMakeTestImageWithAPP1()
        {
            Assert.AreEqual(SOI + APP0 + _testStr + DQT +
                DHT + SOF + SOS + EOI,
                MakeTestImageWithAPP1(_testStr));
        }
    }
}
