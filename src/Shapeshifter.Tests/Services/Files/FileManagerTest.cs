namespace Shapeshifter.WindowsDesktop.Services.Files
{
    using System.IO;
    using System.Text;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    using WindowsDesktop;

    [TestClass]
    public class FileManagerTest: TestBase
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void CanCreateFoldersAndDisposeThemAgain()
        {
            var container = CreateContainer();

            string permanentPath;
            string temporaryPath;
            using (var fileManager = container.Resolve<IFileManager>())
            {
                permanentPath = fileManager.PrepareFolder("Permanent");
                temporaryPath = fileManager.PrepareTemporaryFolder("Temporary");

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
            var container = CreateContainer();
            
            string temporaryPath;
            using (var fileManager = container.Resolve<IFileManager>())
            {
                temporaryPath = fileManager.WriteBytesToTemporaryFile("Temporary.txt", Encoding.Default.GetBytes("hello world"));
                Assert.IsTrue(File.Exists(temporaryPath));
            }
            
            Assert.IsFalse(File.Exists(temporaryPath));
        }

        [TestMethod]
        public void CanGetCommonPathWithBiggestPathFirst()
        {
            var container = CreateContainer();
            var fileManager = container.Resolve<IFileManager>();

            var commonPath = fileManager.FindCommonFolderFromPaths(
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
            var container = CreateContainer();
            var fileManager = container.Resolve<IFileManager>();

            var commonPath = fileManager.FindCommonFolderFromPaths(
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
            var container = CreateContainer();
            var fileManager = container.Resolve<IFileManager>();

            var commonPath = fileManager.FindCommonFolderFromPaths(
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