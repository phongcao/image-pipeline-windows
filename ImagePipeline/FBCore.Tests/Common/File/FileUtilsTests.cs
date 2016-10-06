using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using FBCore.Common.File;
using System.IO;
using Windows.Storage;
using System;

namespace FBCore.Tests.Common.File
{
    /// <summary>
    /// Unit tests for <see cref="FileUtils"/>
    /// </summary>
    [TestClass]
    public class FileUtilsTests
    {
        /// <summary>
        /// Tests out the Mkdirs method
        /// </summary>
        [TestMethod]
        public void TestMkDirsNoWorkRequired()
        {
            // Create temp folder
            DirectoryInfo temp = Directory.CreateDirectory(
                $"{ ApplicationData.Current.LocalFolder.Path }/temp");

            try
            {
                FileUtils.Mkdirs(new DirectoryInfo($"{ ApplicationData.Current.LocalFolder.Path }/temp"));
            }
            catch (CreateDirectoryException)
            {
                Assert.Fail();
            }

            // Cleanup
            temp.Delete();
        }

        /// <summary>
        /// Tests out the Mkdirs method
        /// </summary>
        [TestMethod]
        public void TestMkDirsSuccessfulCreate()
        {
            DirectoryInfo temp = new DirectoryInfo($"{ ApplicationData.Current.LocalFolder.Path }/temp");

            try
            {
                FileUtils.Mkdirs(temp);
            }
            catch (CreateDirectoryException)
            {
                Assert.Fail();
            }

            // Cleanup
            temp.Delete();
        }

        /// <summary>
        /// Tests out the Mkdirs method
        /// </summary>
        [TestMethod]
        public void TestMkDirsCantDeleteExisting()
        {
            // Create temp file
            string tempFileName = Path.Combine(
                ApplicationData.Current.LocalFolder.Path, Guid.NewGuid().ToString());
            FileStream tempFile = System.IO.File.Create(tempFileName);
            bool fileDeleteException = false;

            try
            {
                FileUtils.Mkdirs(new FileInfo(tempFileName));
            }
            catch (FileDeleteException)
            {
                fileDeleteException = true;
            }

            Assert.IsTrue(fileDeleteException);

            // Cleanup
            tempFile.Dispose();
            System.IO.File.Delete(tempFileName);
        }

        /// <summary>
        /// Tests out the Rename method
        /// </summary>
        [TestMethod]
        public void TestRenameSuccessful()
        {
            // Create temp folders
            DirectoryInfo sourceDir = Directory.CreateDirectory(
                $"{ ApplicationData.Current.LocalFolder.Path }/source");
            DirectoryInfo targetDir = new DirectoryInfo(
                $"{ ApplicationData.Current.LocalFolder.Path }/target");

            try
            {
                FileUtils.Rename(sourceDir, targetDir);
            }
            catch (RenameException)
            {
                Assert.Fail();
            }

            Assert.IsTrue(targetDir.Exists);

            // Cleanup
            targetDir.Delete();
        }

        /// <summary>
        /// Tests out the Rename method
        /// </summary>
        [TestMethod]
        public void TestParentDirNotFoundExceptionIsThrown()
        {
            // Create temp folders
            DirectoryInfo sourceDir = new DirectoryInfo(
                $"{ ApplicationData.Current.LocalFolder.Path }/source");
            DirectoryInfo targetDir = new DirectoryInfo(
                $"{ ApplicationData.Current.LocalFolder.Path }/target");

            try
            {
                FileUtils.Rename(sourceDir, targetDir);
                Assert.Fail();
            }
            catch (RenameException)
            {
                // This is expected
            }
        }
    }
}
