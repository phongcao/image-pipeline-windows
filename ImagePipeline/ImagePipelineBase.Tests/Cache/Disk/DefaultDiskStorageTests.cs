using BinaryResource;
using Cache.Common;
using Cache.Disk;
using FBCore.Common.File;
using FBCore.Common.File.Extensions;
using FBCore.Common.Internal;
using FBCore.Common.Time;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Windows.Storage;

namespace ImagePipelineBase.Tests.Cache.Disk
{
    /// <summary>
    /// Tests for 'default' disk storage
    /// </summary>
    [TestClass]
    public class DefaultDiskStorageTests
    {
        private DirectoryInfo _directory;
        private MockSystemClock _clock;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _clock = MockSystemClock.Get();
            StorageFolder cacheDir = ApplicationData.Current.LocalCacheFolder;
            _directory = new DirectoryInfo(Path.Combine(cacheDir.Path, "sharded-disk-storage-test"));
            Assert.IsTrue(_directory.CreateEmpty());
            FileTree.DeleteContents(_directory);
            _clock.SetDateTime(DateTime.Now);
        }

        private ISupplier<DefaultDiskStorage> GetStorageSupplier(int version)
        {
            return new SupplierImpl<DefaultDiskStorage>(() =>
            {
                return new DefaultDiskStorage(
                    _directory,
                    version,
                    NoOpCacheErrorLogger.Instance,
                    _clock);
            });
        }

        /// <summary>
        /// Startup tests
        /// </summary>
        [TestMethod]
        public void TestStartup()
        {
            // Create a bogus file
            FileInfo bogusFile = new FileInfo(Path.Combine(_directory.FullName, "bogus"));
            Assert.IsTrue(bogusFile.CreateEmpty());

            // Create the storage now. Bogus files should be gone now
            DefaultDiskStorage storage = GetStorageSupplier(1).Get();
            Assert.IsFalse(bogusFile.Exists);
            string version1Dir = DefaultDiskStorage.GetVersionSubdirectoryName(1);
            Assert.IsTrue((new DirectoryInfo(Path.Combine(_directory.FullName, version1Dir))).Exists);

            // Create a new version
            storage = GetStorageSupplier(2).Get();
            Assert.IsNotNull(storage);
            Assert.IsFalse((new DirectoryInfo(Path.Combine(_directory.FullName, version1Dir))).Exists);
            string version2Dir = DefaultDiskStorage.GetVersionSubdirectoryName(2);
            Assert.IsTrue((new DirectoryInfo(Path.Combine(_directory.FullName, version2Dir))).Exists);
        }

        /// <summary>
        /// Tests out the Enabled attribute
        /// </summary>
        [TestMethod]
        public void TestEnabled()
        {
            DefaultDiskStorage storage = GetStorageSupplier(1).Get();
            Assert.IsTrue(storage.IsEnabled);
        }

        /// <summary>
        /// Tests basic operations
        /// </summary>
        [TestMethod]
        public void TestBasicOperations()
        {
            DefaultDiskStorage storage = GetStorageSupplier(1).Get();
            string resourceId1 = "R1";
            string resourceId2 = "R2";

            // No file - get should fail
            IBinaryResource resource1 = storage.GetResource(resourceId1, null);
            Assert.IsNull(resource1);

            // Write out the file now
            byte[] key1Contents = new byte[] { 0, 1, 2 };
            WriteToStorage(storage, resourceId1, key1Contents);

            // Get should succeed now
            resource1 = storage.GetResource(resourceId1, null);
            Assert.IsNotNull(resource1);
            FileInfo underlyingFile = ((FileBinaryResource)resource1).File;
            CollectionAssert.AreEqual(key1Contents, Files.ToByteArray(underlyingFile));

            // Remove the file now - get should fail again
            underlyingFile.Delete();

            resource1 = storage.GetResource(resourceId1, null);
            Assert.IsNull(resource1);

            // No file
            IBinaryResource resource2 = storage.GetResource(resourceId2, null);
            Assert.IsNull(resource2);
        }

        /// <summary>
        /// Test that a file is stored in a new file,
        /// and the bytes are stored plainly in the file.
        /// <exception cref="Exception"></exception>
        /// </summary>
        [TestMethod]
        public void TestStoreFile()
        {
            DefaultDiskStorage storage = GetStorageSupplier(1).Get();
            string resourceId1 = "resource1";
            byte[] value1 = new byte[100];
            value1[80] = 101;
            FileInfo file1 = WriteFileToStorage(storage, resourceId1, value1);

            ISet<FileSystemInfo> files = new HashSet<FileSystemInfo>();
            Assert.IsTrue(_directory.Exists);
            IList<FileSystemInfo> founds1 = FindNewFiles(_directory, files, /*recurse*/true);
            Assert.IsNotNull(file1);
            Assert.IsNotNull(founds1.FirstOrDefault(f => f.FullName.Equals(file1.FullName)));
            Assert.IsTrue(file1.Exists);
            Assert.AreEqual(100, file1.Length);
            CollectionAssert.AreEqual(value1, Files.ToByteArray(file1));
        }

        /// <summary>
        /// Inserts 3 files with different dates.
        /// Check what files are there.
        /// Uses an iterator to remove the one in the middle.
        /// Check that later.
        /// <exception cref="Exception"></exception>
        /// </summary>
        [TestMethod]
        public void TestRemoveWithIterator()
        {
            DefaultDiskStorage storage = GetStorageSupplier(1).Get();

            string resourceId1 = "resource1";
            byte[] value1 = new byte[100];
            value1[80] = 101;
            string resourceId2 = "resource2";
            byte[] value2 = new byte[104];
            value2[80] = 102;
            string resourceId3 = "resource3";
            byte[] value3 = new byte[106];
            value3[80] = 103;

            DateTime time1 = DateTime.Now;
            WriteFileToStorage(storage, resourceId1, value1);

            DateTime time2 = time1.AddMilliseconds(1000);
            _clock.SetDateTime(time2);
            WriteFileToStorage(storage, resourceId2, value2);

            _clock.SetDateTime(time2.AddMilliseconds(1000));
            WriteFileToStorage(storage, resourceId3, value3);

            IList<FileSystemInfo> files = FindNewFiles(_directory, new HashSet<FileSystemInfo>(), /*recurse*/true);

            // There should be 1 file per entry
            Assert.AreEqual(3, files.Count);

            // Now delete entry2
            ICollection<IEntry> entries = storage.GetEntries();
            foreach (var item in entries)
            {
                if (Math.Abs((item.Timestamp - time2).TotalMilliseconds) < 500)
                {
                    storage.Remove(item);
                }
            }

            Assert.IsFalse(storage.Contains(resourceId2, null));
            IList<FileSystemInfo> remaining = FindNewFiles(
                _directory, new HashSet<FileSystemInfo>(), /*recurse*/true);

            // 2 entries remain
            Assert.AreEqual(2, remaining.Count);

            // None of them with timestamp close to time2
            IList<IEntry> entries1 = new List<IEntry>(storage.GetEntries());
            Assert.AreEqual(2, entries1.Count);

            // First
            IEntry entry = entries1[0];
            Assert.IsFalse(Math.Abs((entry.Timestamp - time2).TotalMilliseconds) < 500);

            // Second
            entry = entries1[1];
            Assert.IsFalse(Math.Abs((entry.Timestamp - time2).TotalMilliseconds) < 500);
        }

        /// <summary>
        /// Tests out the Touch method
        /// </summary>
        [TestMethod]
        public void TestTouch()
        {
            DefaultDiskStorage storage = GetStorageSupplier(1).Get();
            DateTime startTime = DateTime.Now;

            string resourceId1 = "resource1";
            byte[] value1 = new byte[100];
            FileInfo file1 = WriteFileToStorage(storage, resourceId1, value1);
            Assert.IsTrue(Math.Abs((file1.LastWriteTime- startTime).TotalMilliseconds) <= 500);

            DateTime time2 = startTime.AddMilliseconds(10000);
            _clock.SetDateTime(time2);
            string resourceId2 = "resource2";
            byte[] value2 = new byte[100];
            FileInfo file2 = WriteFileToStorage(storage, resourceId2, value2);
            Assert.IsTrue(Math.Abs((file1.LastWriteTime - startTime).TotalMilliseconds) <= 500);
            Assert.IsTrue(Math.Abs((file2.LastWriteTime - time2).TotalMilliseconds) <= 500);

            DateTime time3 = time2.AddMilliseconds(10000);
            _clock.SetDateTime(time3);
            storage.Touch(resourceId1, null);
            file1.Refresh();
            Assert.IsTrue(Math.Abs((file1.LastWriteTime - time3).TotalMilliseconds) <= 500);
            Assert.IsTrue(Math.Abs((file2.LastWriteTime - time2).TotalMilliseconds) <= 500);
        }

        /// <summary>
        /// Tests out the RemoveById method
        /// </summary>
        [TestMethod]
        public void TestRemoveById()
        {
            DefaultDiskStorage storage = GetStorageSupplier(1).Get();

            string resourceId1 = "resource1";
            byte[] value1 = new byte[100];
            WriteFileToStorage(storage, resourceId1, value1);
            string resourceId2 = "resource2";
            byte[] value2 = new byte[100];
            WriteFileToStorage(storage, resourceId2, value2);
            string resourceId3 = "resource3";
            byte[] value3 = new byte[100];
            WriteFileToStorage(storage, resourceId3, value3);

            Assert.IsTrue(storage.Contains(resourceId1, null));
            Assert.IsTrue(storage.Contains(resourceId2, null));
            Assert.IsTrue(storage.Contains(resourceId3, null));

            storage.Remove(resourceId2);
            Assert.IsTrue(storage.Contains(resourceId1, null));
            Assert.IsFalse(storage.Contains(resourceId2, null));
            Assert.IsTrue(storage.Contains(resourceId3, null));

            storage.Remove(resourceId1);
            Assert.IsFalse(storage.Contains(resourceId1, null));
            Assert.IsFalse(storage.Contains(resourceId2, null));
            Assert.IsTrue(storage.Contains(resourceId3, null));

            storage.Remove(resourceId3);
            Assert.IsFalse(storage.Contains(resourceId1, null));
            Assert.IsFalse(storage.Contains(resourceId2, null));
            Assert.IsFalse(storage.Contains(resourceId3, null));
        }

        /// <summary>
        /// Tests entry id
        /// </summary>
        [TestMethod]
        public void TestEntryIds()
        {
            DefaultDiskStorage storage = GetStorageSupplier(1).Get();

            byte[] value1 = new byte[101];
            byte[] value2 = new byte[102];
            byte[] value3 = new byte[103];
            value1[80] = 123;
            value2[80] = 45;
            value3[80] = 67;
            WriteFileToStorage(storage, "resourceId1", value1);
            WriteFileToStorage(storage, "resourceId2", value2);
            WriteFileToStorage(storage, "resourceId3", value3);

            // Check that resources are retrieved by the right name, before testing getEntries
            IBinaryResource res1 = storage.GetResource("resourceId1", null);
            IBinaryResource res2 = storage.GetResource("resourceId2", null);
            IBinaryResource res3 = storage.GetResource("resourceId3", null);
            CollectionAssert.AreEqual(value1, res1.Read());
            CollectionAssert.AreEqual(value2, res2.Read());
            CollectionAssert.AreEqual(value3, res3.Read());

            // Obtain entries and sort by name
            List<IEntry> entries = new List<IEntry>(storage.GetEntries());
            entries.Sort((a, b) =>
            {
                return a.Id.CompareTo(b.Id);
            });

            Assert.AreEqual(3, entries.Count);
            Assert.AreEqual("resourceId1", entries[0].Id);
            Assert.AreEqual("resourceId2", entries[1].Id);
            Assert.AreEqual("resourceId3", entries[2].Id);
            CollectionAssert.AreEqual(value1, entries[0].Resource.Read());
            CollectionAssert.AreEqual(value2, entries[1].Resource.Read());
            CollectionAssert.AreEqual(value3, entries[2].Resource.Read());
        }

        /// <summary>
        /// Tests immutable entry
        /// </summary>
        [TestMethod]
        public void TestEntryImmutable()
        {
            DefaultDiskStorage storage = GetStorageSupplier(1).Get();

            string resourceId1 = "resource1";
            byte[] value1 = new byte[100];
            value1[80] = 123;
            FileInfo file1 = WriteFileToStorage(storage, resourceId1, value1);

            Assert.AreEqual(100, file1.Length);
            ICollection<IEntry> entries = storage.GetEntries();
            IEntry entry = entries.FirstOrDefault();
            DateTime timestamp = entry.Timestamp;
            _clock.SetDateTime(DateTime.Now.AddHours(1));
            storage.GetResource(resourceId1, null);

            // Now the new timestamp show be higher, but the entry should have the same value
            ICollection<IEntry> newEntries = storage.GetEntries();
            IEntry newEntry = newEntries.FirstOrDefault();
            Assert.IsTrue(timestamp < newEntry.Timestamp);
            Assert.AreEqual(timestamp, entry.Timestamp);
        }

        /// <summary>
        /// Tests temp file eviction
        /// </summary>
        [TestMethod]
        public void TestTempFileEviction()
        {
            DefaultDiskStorage storage = GetStorageSupplier(1).Get();

            string resourceId1 = "resource1";
            IInserter inserter = storage.Insert(resourceId1, null);
            FileInfo tempFile = ((DefaultDiskStorage.InserterImpl)inserter)._temporaryFile;

            // Make sure that we don't evict a recent temp file
            PurgeUnexpectedFiles(storage);
            Assert.IsTrue(tempFile.Exists);

            // Mark it old, then try eviction again. It should be gone.
            try
            {
                tempFile.LastWriteTime = _clock.Now.Subtract(
                    TimeSpan.FromMilliseconds(DefaultDiskStorage.TEMP_FILE_LIFETIME_MS + 1000));
            }
            catch (Exception)
            {
                throw new IOException("Unable to update timestamp of file: " + tempFile);
            }

            PurgeUnexpectedFiles(storage);
            Assert.IsFalse(tempFile.Exists);
        }

        /// <summary>
        /// Test that purgeUnexpectedResources deletes all files/directories outside the version directory
        /// but leaves untouched the version directory and the content files.
        /// <exception cref="Exception"></exception>
        /// </summary>
        [TestMethod]
        public void TestPurgeUnexpectedFiles()
        {
            DefaultDiskStorage storage = GetStorageSupplier(1).Get();

            string resourceId = "file1";
            byte[] CONTENT = Encoding.UTF8.GetBytes("content");

            FileInfo file = WriteFileToStorage(storage, resourceId, CONTENT);

            // Check file exists
            Assert.IsTrue(file.Exists);
            CollectionAssert.AreEqual(CONTENT, Files.ToByteArray(file));

            FileInfo unexpectedFile1 = new FileInfo(
                Path.Combine(_directory.FullName, "unexpected-file-1"));
            FileInfo unexpectedFile2 = new FileInfo(
                Path.Combine(_directory.FullName, "unexpected-file-2"));
            Assert.IsTrue(unexpectedFile1.CreateEmpty());
            Assert.IsTrue(unexpectedFile2.CreateEmpty());

            DirectoryInfo unexpectedDir1 = new DirectoryInfo(
                Path.Combine(_directory.FullName, "unexpected-dir-1"));
            DirectoryInfo unexpectedDir2 = new DirectoryInfo(
                Path.Combine(_directory.FullName, "unexpected-dir-2"));
            Assert.IsTrue(unexpectedDir1.CreateEmpty());
            Assert.IsTrue(unexpectedDir2.CreateEmpty());

            FileInfo unexpectedSubfile1 = new FileInfo(
                Path.Combine(unexpectedDir2.FullName, "unexpected-sub-file-1"));
            Assert.IsTrue(unexpectedSubfile1.CreateEmpty());
            Assert.AreEqual(5, _directory.ListFiles().Length); // 4 unexpected (files+dirs) + ver. dir
            Assert.AreEqual(1, unexpectedDir2.ListFiles().Length);
            Assert.AreEqual(0, unexpectedDir1.ListFiles().Length);

            FileInfo unexpectedFileInShard = new FileInfo(
                Path.Combine(file.GetParent().FullName, "unexpected-in-shard"));
            Assert.IsTrue(unexpectedFileInShard.CreateEmpty());

            storage.PurgeUnexpectedResources();
            unexpectedFile1.Refresh();
            unexpectedFile2.Refresh();
            unexpectedSubfile1.Refresh();
            unexpectedDir1.Refresh();
            unexpectedDir2.Refresh();
            Assert.IsFalse(unexpectedFile1.Exists);
            Assert.IsFalse(unexpectedFile2.Exists);
            Assert.IsFalse(unexpectedSubfile1.Exists);
            Assert.IsFalse(unexpectedDir1.Exists);
            Assert.IsFalse(unexpectedDir2.Exists);

            // Check file still exists
            Assert.IsTrue(file.Exists);

            // Check unexpected sibling is gone
            Assert.IsFalse(unexpectedFileInShard.Exists);

            // Check the only thing in root is the version directory
            Assert.AreEqual(1, _directory.ListFiles().Length); // just the version directory
        }

        /// <summary>
        /// Tests that an existing directory is nuked when it's not current version (doens't have
        /// the version directory used for the structure)
        /// <exception cref="Exception"></exception>
        /// </summary>
        [TestMethod]
        public void TestDirectoryIsNuked()
        {
            Assert.AreEqual(0, _directory.ListFiles().Length);

            // Create file before setting final test date
            FileInfo somethingArbitrary = new FileInfo(
                Path.Combine(_directory.FullName, "something-arbitrary"));
            Assert.IsTrue(somethingArbitrary.CreateEmpty());
            long lastModified = (_directory.LastWriteTime.Ticks / TimeSpan.TicksPerMillisecond) - 1000;
            _directory.LastWriteTime = new DateTime(lastModified * TimeSpan.TicksPerMillisecond);

            // Check it was changed
            Assert.AreEqual(lastModified * TimeSpan.TicksPerMillisecond, _directory.LastWriteTime.Ticks);

            GetStorageSupplier(1).Get();
            _directory.Refresh();

            // _directory exists...
            Assert.IsTrue(_directory.Exists);

            // But it was created now
            Assert.IsTrue(lastModified * TimeSpan.TicksPerMillisecond < _directory.LastWriteTime.Ticks);
        }

        /// <summary>
        /// Tests that an existing directory is not nuked if the version directory used for the structure
        /// exists (so it's current version and doesn't suffer Samsung RFS problem)
        /// <exception cref="Exception"></exception>
        /// </summary>
        [TestMethod]
        public void TestDirectoryIsNotNuked()
        {
            Assert.AreEqual(0, _directory.ListFiles().Length);

            DefaultDiskStorage storage = GetStorageSupplier(1).Get();
            string resourceId = "file1";

            byte[] CONTENT = Encoding.UTF8.GetBytes("content");

            // Create a file so we know version directory really exists
            IInserter inserter = storage.Insert(resourceId, null);
            WriteToResource(inserter, CONTENT);
            inserter.Commit(null);

            // Assign some previous date to the "now" used for file creation
            long lastModified = _directory.LastWriteTime.Ticks / TimeSpan.TicksPerMillisecond - 1000;
            _directory.LastWriteTime = new DateTime(lastModified * TimeSpan.TicksPerMillisecond);

            // Check it was changed
            Assert.AreEqual(lastModified * TimeSpan.TicksPerMillisecond, _directory.LastWriteTime.Ticks);

            // Create again, it shouldn't delete the directory
            GetStorageSupplier(1).Get();
            _directory.Refresh();

            // _directory exists...
            Assert.IsTrue(_directory.Exists);

            // And it's the same as before
            Assert.AreEqual(lastModified * TimeSpan.TicksPerMillisecond, _directory.LastWriteTime.Ticks);
        }

        /// <summary>
        /// Test the iterator returned is ok and deletion through the iterator is ok too.
        /// This is the required functionality that eviction needs.
        /// <exception cref="Exception"></exception>
        /// </summary>
        [TestMethod]
        public void TestIterationAndRemoval()
        {
            DefaultDiskStorage storage = GetStorageSupplier(1).Get();
            string resourceId0 = "file0";
            string resourceId1 = "file1";
            string resourceId2 = "file2";
            string resourceId3 = "file3";

            byte[] CONTENT0 = Encoding.UTF8.GetBytes("content0");
            byte[] CONTENT1 = Encoding.UTF8.GetBytes("content1-bigger");
            byte[] CONTENT2 = Encoding.UTF8.GetBytes("content2");
            byte[] CONTENT3 = Encoding.UTF8.GetBytes("content3-biggest");

            IList<FileSystemInfo> files = new List<FileSystemInfo>(4);

            DateTime time1 = DateTime.Now;
            files.Add(Write(storage, resourceId0, CONTENT0));

            DateTime time2 = time1.AddMilliseconds(1000);
            _clock.SetDateTime(time2);
            files.Add(Write(storage, resourceId1, CONTENT1));

            DateTime time3 = time2.AddMilliseconds(1000);
            _clock.SetDateTime(time3);
            files.Add(Write(storage, resourceId2, CONTENT2));

            DateTime time4 = time3.AddMilliseconds(1000);
            _clock.SetDateTime(time4);
            files.Add(Write(storage, resourceId3, CONTENT3));

            IList<DefaultDiskStorage.EntryImpl> entries = RetrieveEntries(storage);
            Assert.AreEqual(4, entries.Count);
            Assert.AreEqual(files[0].FullName, ((FileBinaryResource)entries[0].Resource).File.FullName);
            Assert.AreEqual(files[1].FullName, ((FileBinaryResource)entries[1].Resource).File.FullName);
            Assert.AreEqual(files[2].FullName, ((FileBinaryResource)entries[2].Resource).File.FullName);
            Assert.AreEqual(files[3].FullName, ((FileBinaryResource)entries[3].Resource).File.FullName);

            // Try the same after removing 2 entries
            foreach (IEntry entry in storage.GetEntries())
            {
                // Delete the 2 biggest files: key1 and key3 (see the content values)
                if (entry.GetSize() >= CONTENT1.Length)
                {
                    storage.Remove(entry);
                }
            }

            IList<DefaultDiskStorage.EntryImpl> entriesAfterRemoval = RetrieveEntries(storage);
            Assert.AreEqual(2, entriesAfterRemoval.Count);
            Assert.AreEqual(files[0].FullName, 
                ((FileBinaryResource)entriesAfterRemoval[0].Resource).File.FullName);
            Assert.AreEqual(files[2].FullName, 
                ((FileBinaryResource)entriesAfterRemoval[1].Resource).File.FullName);
        }

        private static FileBinaryResource WriteToStorage(
            DefaultDiskStorage storage,
            string resourceId,
            byte[] value)
        {
            IInserter inserter = storage.Insert(resourceId, null);
            WriteToResource(inserter, value);
            return (FileBinaryResource)inserter.Commit(null);
        }

        private static FileInfo WriteFileToStorage(
            DefaultDiskStorage storage,
            string resourceId,
            byte[] value)
        {
            return WriteToStorage(storage, resourceId, value).File;
        }

        private static FileInfo Write(
            DefaultDiskStorage storage,
            string resourceId,
            byte[] content)
        {
            IInserter inserter = storage.Insert(resourceId, null);
            FileInfo file = ((DefaultDiskStorage.InserterImpl)inserter)._temporaryFile;
            FileStream fos = file.Create();
            try
            {
                fos.Write(content, 0, content.Length);
            }
            finally
            {
                fos.Dispose();
            }

            return ((FileBinaryResource)inserter.Commit(null)).File;
        }

        private static void WriteToResource(
            IInserter inserter,
            byte[] content)
        {
            inserter.WriteData(
                new WriterCallbackImpl(
                    os =>
                    {
                        os.Write(content, 0, content.Length);
                    }),
                    null);
        }

        private void PurgeUnexpectedFiles(DefaultDiskStorage storage)
        {
            storage.PurgeUnexpectedResources();
        }

        private IList<FileSystemInfo> FindNewFiles(FileSystemInfo directory, ISet<FileSystemInfo> existing, bool recurse)
        {
            List<FileSystemInfo> result = new List<FileSystemInfo>();
            FindNewFiles(directory, existing, recurse, result);
            return result;
        }

        private void FindNewFiles(
            FileSystemInfo directory,
            ISet<FileSystemInfo> existing,
            bool recurse,
            IList<FileSystemInfo> result)
        {
            FileSystemInfo[] files = directory.ListFiles();
            if (files != null)
            {
                foreach (var file in files)
                {
                    if (file.IsDirectory() && recurse)
                    {
                        FindNewFiles(file, existing, true, result);
                    }
                    else if (!existing.Contains(file))
                    {
                        result.Add(file);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a list of entries (the one returned by DiskStorage.Session.entriesIterator)
        /// ordered by timestamp.
        /// <param name="storage"></param>
        /// </summary>
        private static IList<DefaultDiskStorage.EntryImpl> RetrieveEntries(
            DefaultDiskStorage storage)
        {
            List<IEntry> entries = new List<IEntry>(storage.GetEntries());

            entries.Sort((a, b) =>
            {
                DateTime al = a.Timestamp;
                DateTime bl = b.Timestamp;
                return (al < bl) ? -1 : ((al > bl) ? 1 : 0);
            });

            IList<DefaultDiskStorage.EntryImpl> newEntries = new List<DefaultDiskStorage.EntryImpl>();
            foreach (IEntry entry in entries) 
            {
              newEntries.Add((DefaultDiskStorage.EntryImpl) entry);
            }

            return newEntries;
        }
    }
}
