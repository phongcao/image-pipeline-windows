using FBCore.Common.Statfs;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Linq;
using Windows.Storage;

namespace FBCore.Tests.Common.Statfs
{
    /// <summary>
    /// Tests for <see cref="StatFsHelper"/>.
    /// </summary>
    [TestClass]
    public class StatFsHelperTests
    {
        private const int INTERNAL_BLOCK_SIZE = 512;
        private const int EXTERNAL_BLOCK_SIZE = 2048;
        private const int INTERNAL_BLOCKS_FREE = 16;
        private const int EXTERNAL_BLOCKS_FREE = 32;
        private const int INTERNAL_FREE_BYTES = INTERNAL_BLOCK_SIZE * INTERNAL_BLOCKS_FREE;
        private const int EXTERNAL_FREE_BYTES = EXTERNAL_BLOCK_SIZE * EXTERNAL_BLOCKS_FREE;

        private StorageFolder _internalPath;
        private StorageFolder _externalPath;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _internalPath = ApplicationData.Current.LocalFolder;

            if (!KnownFolders.RemovableDevices.Path.Equals(string.Empty))
            {
                _externalPath = KnownFolders.RemovableDevices.GetFoldersAsync().GetAwaiter().GetResult().FirstOrDefault();
            }
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            _internalPath = null;
            _externalPath = null;
            MockStatFsHelper.Instance.SetInternalPath(_internalPath);
            MockStatFsHelper.Instance.SetExternalPath(_externalPath);
            MockStatFsHelper.Instance.SetInternalStatFs(null);
            MockStatFsHelper.Instance.SetExternalStatFs(null);
        }

        /// <summary>
        /// Tests out the GetAvailableStorageSpace method
        /// </summary>
        [TestMethod]
        public void TestGetAvailableStorageSpace()
        {
            Assert.IsTrue(
                StatFsHelper.Instance.GetAvailableStorageSpace(StatFsHelper.StorageType.INTERNAL) > 0);

            if (_externalPath != null)
            {
                Assert.IsTrue(
                    StatFsHelper.Instance.GetAvailableStorageSpace(StatFsHelper.StorageType.EXTERNAL) > 0);
            }
        }

        /// <summary>
        /// Tests the statFs creation
        /// </summary>
        [TestMethod]
        public void TestShouldCreateStatFsForInternalAndExternalStorage()
        {
            ExpectInternalSetup();
            ExpectExternalSetup();

            MockStatFsHelper statFsHelper = MockStatFsHelper.Instance;

            ulong freeBytes = statFsHelper.GetAvailableStorageSpace(StatFsHelper.StorageType.INTERNAL);
            Assert.IsTrue(INTERNAL_FREE_BYTES == freeBytes);

            freeBytes = statFsHelper.GetAvailableStorageSpace(StatFsHelper.StorageType.EXTERNAL);
            Assert.IsTrue(EXTERNAL_FREE_BYTES == freeBytes);
        }

        /// <summary>
        /// Tests the statFs creation for internal storage only
        /// </summary>
        [TestMethod]
        public void TestShouldCreateStatFsForInternalStorageOnly()
        {
            ExpectInternalSetup();

            MockStatFsHelper statFsHelper = MockStatFsHelper.Instance;

            ulong freeBytes = statFsHelper.GetAvailableStorageSpace(StatFsHelper.StorageType.INTERNAL);
            Assert.IsTrue(INTERNAL_FREE_BYTES == freeBytes);

            freeBytes = statFsHelper.GetAvailableStorageSpace(StatFsHelper.StorageType.EXTERNAL);
            Assert.IsTrue(0 == freeBytes);
        }

        /// <summary>
        /// Tests no internal storage
        /// </summary>
        [TestMethod]
        public void TestShouldHandleNoInternalStorage()
        {
            MockStatFsHelper statFsHelper = MockStatFsHelper.Instance;

            ulong freeBytes = statFsHelper.GetAvailableStorageSpace(StatFsHelper.StorageType.INTERNAL);
            Assert.IsTrue(0 == freeBytes);

            freeBytes = statFsHelper.GetAvailableStorageSpace(StatFsHelper.StorageType.EXTERNAL);
            Assert.IsTrue(0 == freeBytes);
        }

        /// <summary>
        /// Tests handling external storage removed
        /// </summary>
        [TestMethod]
        public void TestShouldHandleExternalStorageRemoved()
        {
            ExpectInternalSetup();
            ExpectExternalSetup();

            MockStatFsHelper statFsHelper = MockStatFsHelper.Instance;
            statFsHelper.SetExternalPath(null);
            statFsHelper.ResetStats();

            ulong freeBytes = statFsHelper.GetAvailableStorageSpace(StatFsHelper.StorageType.INTERNAL);
            Assert.IsTrue(INTERNAL_FREE_BYTES == freeBytes);

            freeBytes = statFsHelper.GetAvailableStorageSpace(StatFsHelper.StorageType.EXTERNAL);
            Assert.IsTrue(0 == freeBytes);
        }
        /// <summary>
        /// Tests handling external storage reinserted
        /// </summary>
        [TestMethod]
        public void TestShouldHandleExternalStorageReinserted()
        {
            ExpectInternalSetup();
            ExpectExternalSetup();

            MockStatFsHelper statFsHelper = MockStatFsHelper.Instance;
            statFsHelper.SetExternalPath(null);
            statFsHelper.ResetStats();

            ulong freeBytes = statFsHelper.GetAvailableStorageSpace(StatFsHelper.StorageType.INTERNAL);
            Assert.IsTrue(INTERNAL_FREE_BYTES == freeBytes);

            freeBytes = statFsHelper.GetAvailableStorageSpace(StatFsHelper.StorageType.EXTERNAL);
            Assert.IsTrue(0 == freeBytes);

            statFsHelper.SetExternalPath(_externalPath);
            statFsHelper.SetExternalStatFs(EXTERNAL_FREE_BYTES);
            statFsHelper.ResetStats();

            freeBytes = statFsHelper.GetAvailableStorageSpace(StatFsHelper.StorageType.EXTERNAL);
            Assert.IsTrue(EXTERNAL_FREE_BYTES == freeBytes);

            statFsHelper.ResetStats();

            freeBytes = statFsHelper.GetAvailableStorageSpace(StatFsHelper.StorageType.EXTERNAL);
            Assert.IsTrue(EXTERNAL_FREE_BYTES == freeBytes);
        }

        private void ExpectInternalSetup()
        {
            _internalPath = ApplicationData.Current.LocalFolder;
            MockStatFsHelper.Instance.SetInternalPath(_internalPath);
            MockStatFsHelper.Instance.SetInternalStatFs(INTERNAL_FREE_BYTES);
        }

        private void ExpectExternalSetup()
        {
            _externalPath = ApplicationData.Current.LocalCacheFolder;
            MockStatFsHelper.Instance.SetExternalPath(_externalPath);
            MockStatFsHelper.Instance.SetExternalStatFs(EXTERNAL_FREE_BYTES);
        }
    }
}
