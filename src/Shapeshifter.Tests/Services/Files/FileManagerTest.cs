namespace Shapeshifter.WindowsDesktop.Services.Files
{
    using System.IO;
    using System.Text;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FileManagerTest: UnitTestFor<IFileManager>
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void CanCreateFoldersAndDisposeThemAgain()
        {
            string permanentPath;
            string temporaryPath;
            using (systemUnderTest)
            {
                permanentPath = systemUnderTest.PrepareIsolatedFolder(
                    "Permanent");
                temporaryPath = systemUnderTest.PrepareTemporaryFolder(
                    "Temporary");

                Assert.IsTrue(Directory.Exists(permanentPath));
                Assert.IsTrue(Directory.Exists(temporaryPath));
            }

            Assert.IsTrue(Directory.Exists(permanentPath));
            Assert.IsFalse(Directory.Exists(temporaryPath));

            Directory.Delete(permanentPath);

            Assert.IsFalse(Directory.Exists(permanentPath));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void CanCreateFilesAndDisposeThemAgain()
        {
            string temporaryPath;
            using (systemUnderTest)
            {
                temporaryPath = systemUnderTest.WriteBytesToTemporaryFile(
                    "Temporary.txt", 
                    Encoding.Default.GetBytes("hello world"));
                Assert.IsTrue(File.Exists(temporaryPath));
            }

            Assert.IsFalse(File.Exists(temporaryPath));
        }

        [TestMethod]
        public void CanGetCommonPathWithBiggestPathFirst()
        {
            var commonPath = systemUnderTest.FindCommonFolderFromPaths(
                new[]
                {
                    "foo\\bar\\baz\\lol",
                    "foo\\bar\\baz",
                    "foo\\bar"
                });

            Assert.AreEqual("foo\\bar", commonPath);
        }

        [TestMethod]
        public void CanGetCommonPathWithSmallestPathFirst()
        {
            var commonPath = systemUnderTest.FindCommonFolderFromPaths(
                new[]
                {
                    "foo\\bar",
                    "foo\\bar\\baz",
                    "foo\\bar\\baz\\lol"
                });

            Assert.AreEqual("foo\\bar", commonPath);
        }

        [TestMethod]
        public void CanGetCommonPathWithBiggestPathInMiddle()
        {
            var commonPath = systemUnderTest.FindCommonFolderFromPaths(
                new[]
                {
                    "foo\\bar",
                    "foo\\bar\\baz\\lol",
                    "foo\\bar\\baz"
                });

            Assert.AreEqual("foo\\bar", commonPath);
        }
    }
}