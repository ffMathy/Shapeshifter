namespace Shapeshifter.WindowsDesktop.Services.Files
{
    using System.IO;
    using System.Text;
	using System.Threading.Tasks;
	using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FileManagerTest: UnitTestFor<IFileManager>
    {
        [TestMethod]
        [TestCategory("Integration")]
        public async Task CanCreateFoldersAndDisposeThemAgain()
        {
            string permanentPath;
            string temporaryPath;
            using (SystemUnderTest)
            {
                permanentPath = SystemUnderTest.PrepareIsolatedFolder(
                    "Permanent");
                temporaryPath = await SystemUnderTest.PrepareTemporaryFolderAsync(
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
        public async Task CanCreateFilesAndDisposeThemAgain()
        {
            string temporaryPath;
            using (SystemUnderTest)
            {
                temporaryPath = await SystemUnderTest
					.WriteBytesToTemporaryFileAsync(
						"Temporary.txt", 
						Encoding.Default.GetBytes("hello world"));
                Assert.IsTrue(File.Exists(temporaryPath));
            }

            Assert.IsFalse(File.Exists(temporaryPath));
        }

        [TestMethod]
        public void CanGetCommonPathWithBiggestPathFirst()
        {
            var commonPath = SystemUnderTest.FindCommonFolderFromPaths(
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
            var commonPath = SystemUnderTest.FindCommonFolderFromPaths(
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
            var commonPath = SystemUnderTest.FindCommonFolderFromPaths(
                new[]
                {
                    "foo\\bar",
                    "foo\\bar\\baz\\lol",
                    "foo\\bar\\baz"
                });

            Assert.AreEqual("foo\\bar", commonPath);
        }

        [TestMethod]
        public void CanGetCommonPathWhenItIsShorterThanAnyOfThePaths()
        {
            var commonPath = SystemUnderTest.FindCommonFolderFromPaths(
                new[]
                {
                    "foo\\bar\\foo",
                    "foo\\bar\\baz\\lol",
                    "foo\\bar\\baz"
                });

            Assert.AreEqual("foo\\bar", commonPath);
        }

        [TestMethod]
        public void CanGetCommonPathWhenSeparatorsDiffer()
        {
            var commonPath = SystemUnderTest.FindCommonFolderFromPaths(
                new[]
                {
                    "foo\\bar\\foo",
                    "foo/bar/baz/lol",
                    "foo/bar\\baz"
                });

            Assert.AreEqual("foo\\bar", commonPath);
        }
    }
}