using Cache.Common;
using Cache.Disk;
using FBCore.Common.File;
using FBCore.Common.File.Extensions;
using FBCore.Common.Internal;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.IO;
using Windows.Storage;

namespace ImagePipelineBase.Tests.Cache.Disk
{
    /// <summary>
    /// Test out methods in DynamicDefaultDiskStorage
    /// </summary>
    [TestClass]
    public class DynamicDefaultDiskStorageTests
    {
        private int _version;
        private string _baseDirectoryName;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _version = 1;
            _baseDirectoryName = "base";
        }

        private DynamicDefaultDiskStorage CreateStorage(bool useFilesDirInsteadOfCacheDir)
        {
            return new DynamicDefaultDiskStorage(
                _version,
                useFilesDirInsteadOfCacheDir ?
                    Suppliers.of<FileSystemInfo>(
                        new DirectoryInfo(ApplicationData.Current.LocalFolder.Path)) :
                    Suppliers.of<FileSystemInfo>(
                        new DirectoryInfo(ApplicationData.Current.LocalCacheFolder.Path)),
                _baseDirectoryName,
                NoOpCacheErrorLogger.Instance);
        }

        private DynamicDefaultDiskStorage CreateInternalCacheDirStorage()
        {
            return CreateStorage(false);
        }

        private DynamicDefaultDiskStorage CreateInternalFilesDirStorage()
        {
            return CreateStorage(true);
        }

        private static DirectoryInfo GetStorageSubdirectory(DirectoryInfo rootDir, int version)
        {
            return new DirectoryInfo(
                Path.Combine(rootDir.FullName, DefaultDiskStorage.GetVersionSubdirectoryName(version)));
        }

        /// <summary>
        /// Tests the internal local cache directory
        /// </summary>
        [TestMethod]
        public void TestGet_InternalCacheDir()
        {
            DirectoryInfo cacheDir = new DirectoryInfo(ApplicationData.Current.LocalCacheFolder.Path);

            DynamicDefaultDiskStorage storage = CreateInternalCacheDirStorage();

            // Initial state
            Assert.IsNull(storage._currentState.DiskStorageDelegate);

            // After first initialization
            IDiskStorage diskStorageDelegate = storage.Get();
            Assert.AreEqual(diskStorageDelegate, storage._currentState.DiskStorageDelegate);
            Assert.IsTrue(diskStorageDelegate.GetType() == typeof(DefaultDiskStorage));

            DirectoryInfo baseDir = new DirectoryInfo(Path.Combine(cacheDir.FullName, _baseDirectoryName));
            Assert.IsTrue(baseDir.Exists);
            Assert.IsTrue(GetStorageSubdirectory(baseDir, 1).Exists);

            // no change => should get back the same storage instance
            IDiskStorage storage2 = storage.Get();
            Assert.AreEqual(diskStorageDelegate, storage2);

            // Root directory has been moved (proxy for delete). So we should get back a different instance
            DirectoryInfo baseDirOrig = new DirectoryInfo(baseDir.FullName);
            Assert.IsTrue(baseDirOrig.RenameTo(new DirectoryInfo(
                Path.Combine(cacheDir.FullName, "dummydir"))));
            IDiskStorage storage3 = storage.Get();
            Assert.AreNotEqual(diskStorageDelegate, storage3);
            Assert.IsTrue(storage3.GetType() == typeof(DefaultDiskStorage));
            baseDir.Refresh();
            Assert.IsTrue(baseDir.Exists);
            Assert.IsTrue(GetStorageSubdirectory(baseDir, 1).Exists);
        }

        /// <summary>
        /// Tests the internal local directory
        /// </summary>
        [TestMethod]
        public void TestGet_InternalFilesDir()
        {
            DirectoryInfo cacheDir = new DirectoryInfo(ApplicationData.Current.LocalFolder.Path);

            DynamicDefaultDiskStorage supplier = CreateInternalFilesDirStorage();

            // Initial state
            Assert.IsNull(supplier._currentState.DiskStorageDelegate);

            // After first initialization
            IDiskStorage storage = supplier.Get();
            Assert.AreEqual(storage, supplier._currentState.DiskStorageDelegate);
            Assert.IsTrue(storage.GetType() == typeof(DefaultDiskStorage));

            DirectoryInfo baseDir = new DirectoryInfo(Path.Combine(cacheDir.FullName, _baseDirectoryName));
            Assert.IsTrue(baseDir.Exists);
            Assert.IsTrue(GetStorageSubdirectory(baseDir, 1).Exists);

            // No change => should get back the same storage instance
            IDiskStorage storage2 = supplier.Get();
            Assert.AreEqual(storage, storage2);

            DirectoryInfo baseDirOrig = new DirectoryInfo(baseDir.FullName);
            Assert.IsTrue(baseDirOrig.RenameTo(new DirectoryInfo(
                Path.Combine(cacheDir.FullName, "dummydir"))));
            IDiskStorage storage3 = supplier.Get();
            Assert.AreNotEqual(storage, storage3);
            Assert.IsTrue(storage3.GetType() == typeof(DefaultDiskStorage));
            baseDir.Refresh();
            Assert.IsTrue(baseDir.Exists);
            Assert.IsTrue(GetStorageSubdirectory(baseDir, 1).Exists);
        }

        /// <summary>
        /// Tests the root directory creation if necessary
        /// </summary>
        [TestMethod]
        public void TestCreateRootDirectoryIfNecessary()
        {
            DynamicDefaultDiskStorage supplier = CreateInternalCacheDirStorage();
            Assert.IsNull(supplier._currentState.DiskStorageDelegate);
            DirectoryInfo cacheDir = new DirectoryInfo(ApplicationData.Current.LocalCacheFolder.Path);
            DirectoryInfo baseDir = new DirectoryInfo(Path.Combine(cacheDir.FullName, _baseDirectoryName));

            // Directory is clean
            supplier.CreateRootDirectoryIfNecessary(baseDir);
            baseDir.Refresh();
            Assert.IsTrue(baseDir.Exists);

            // Cleanup
            FileTree.DeleteRecursively(baseDir);

            // A file with the same name exists - this should clobber the file, and create a directory
            // instead
            FileInfo dummyFile = new FileInfo(Path.Combine(cacheDir.FullName, _baseDirectoryName));
            Assert.IsTrue(dummyFile.CreateEmpty());
            Assert.IsTrue(dummyFile.Exists);
            supplier.CreateRootDirectoryIfNecessary(baseDir);
            baseDir.Refresh();
            Assert.IsTrue(baseDir.Exists);

            // Cleanup
            FileTree.DeleteRecursively(baseDir);

            // A directory with the same name exists - and with a file in it.
            // Everything should stay the same
            baseDir.Create();
            FileInfo dummyFile2 = new FileInfo(Path.Combine(baseDir.FullName, "dummy1"));
            Assert.IsTrue(dummyFile2.CreateEmpty());
            Assert.IsTrue(dummyFile2.Exists);
            supplier.CreateRootDirectoryIfNecessary(baseDir);
            baseDir.Refresh();
            dummyFile2.Refresh();
            Assert.IsTrue(dummyFile2.Exists);
        }

        /// <summary>
        /// Tests out the delete method
        /// </summary>
        [TestMethod]
        public void TestDeleteStorage()
        {
            DynamicDefaultDiskStorage storage = CreateInternalCacheDirStorage();
            Assert.IsNull(storage._currentState.DiskStorageDelegate);
            storage.DeleteOldStorageIfNecessary();

            storage.Get();
            DirectoryInfo cacheDir = new DirectoryInfo(ApplicationData.Current.LocalCacheFolder.Path);
            DirectoryInfo versionDir = GetStorageSubdirectory(
                new DirectoryInfo(Path.Combine(cacheDir.FullName, _baseDirectoryName)), _version);
            Assert.IsTrue(versionDir.Exists);
            FileInfo dummyFile = new FileInfo(Path.Combine(versionDir.FullName, "dummy"));
            Assert.IsTrue(dummyFile.CreateEmpty());
            Assert.IsTrue(dummyFile.Exists);
            storage.DeleteOldStorageIfNecessary();
            dummyFile.Refresh();
            versionDir.Refresh();
            Assert.IsFalse(dummyFile.Exists);
            Assert.IsFalse(versionDir.Exists);
            Assert.IsFalse(versionDir.Parent.Exists);
        }

        /// <summary>
        /// Tests out the create method
        /// </summary>
        [TestMethod]
        public void TestCreateStorage()
        {
            DynamicDefaultDiskStorage storage = CreateInternalCacheDirStorage();

            DirectoryInfo cacheDir = new DirectoryInfo(ApplicationData.Current.LocalCacheFolder.Path);
            DirectoryInfo baseDir = new DirectoryInfo(Path.Combine(cacheDir.FullName, _baseDirectoryName));
            DirectoryInfo versionDir = GetStorageSubdirectory(baseDir, _version);

            Assert.IsFalse(versionDir.Exists);
            Assert.IsFalse(baseDir.Exists);
            storage.Get();
            versionDir.Refresh();
            baseDir.Refresh();
            Assert.IsTrue(baseDir.Exists);
            Assert.IsTrue(versionDir.Exists);
        }
    }
}
