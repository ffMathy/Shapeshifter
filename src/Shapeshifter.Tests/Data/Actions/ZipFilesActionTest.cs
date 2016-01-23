namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;

    using Autofac;

    using Data.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Clipboard.Interfaces;

    [TestClass]
    public class ZipFilesActionTest: ActionTestBase<IZipFilesAction>
    {
        [TestMethod]
        public async Task CanPerformForFiles()
        {
            Assert.IsTrue(
                await systemUnderTest.CanPerformAsync(GetPackageContaining<IClipboardFileData>()));
        }

        [TestMethod]
        public async Task CanPerformForFileCollections()
        {
            var fakeData = GetPackageContaining<IClipboardFileCollectionData>();
            Assert.IsTrue(await systemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public void CanGetDescription()
        {
            Assert.IsNotNull(systemUnderTest.Description);
        }

        [TestMethod]
        public void CanGetTitle()
        {
            Assert.IsNotNull(systemUnderTest.Title);
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(75, systemUnderTest.Order);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        [TestCategory("Integration")]
        public async Task ThrowsExceptionForInvalidData()
        {
            var fakeData = Substitute.For<IClipboardData>();

            var package = new ClipboardDataPackage();
            package.AddData(fakeData);

            await systemUnderTest.PerformAsync(package);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        [TestCategory("Integration")]
        public async Task NoFilesAddedThrowsException()
        {
            var fakeDataSourceService = Substitute.For<IDataSourceService>();

            var fileCollectionData = new ClipboardFileCollectionData(fakeDataSourceService)
            {
                Files = new Collection<IClipboardFileData>()
            };

            var package = new ClipboardDataPackage();
            package.AddData(fileCollectionData);

            await systemUnderTest.PerformAsync(package);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task ProducesProperZipFileForCollection()
        {
            var file1 = Path.GetTempFileName();
            var file2 = Path.GetTempFileName();

            File.WriteAllText(file1, "file1");
            File.WriteAllText(file2, "file2");

            var fakeDataSourceService = Substitute.For<IDataSourceService>();

            var fileCollectionData = new ClipboardFileCollectionData(fakeDataSourceService)
            {
                Files = new[]
                {
                    new ClipboardFileData(fakeDataSourceService)
                    {
                        FullPath = file1,
                        FileName = Path.GetFileName(file1)
                    },
                    new ClipboardFileData(fakeDataSourceService)
                    {
                        FullPath = file2,
                        FileName = Path.GetFileName(file2)
                    }
                }
            };

            var package = new ClipboardDataPackage();
            package.AddData(fileCollectionData);

            string zipPath = null;
            container.Resolve<IClipboardInjectionService>()
                .When(x => x.InjectFiles(Arg.Any<string[]>()))
                .Do(
                    parameters => {
                        var files = (string[]) parameters[0];
                        zipPath = files[0];
                    });

            await systemUnderTest.PerformAsync(package);

            Assert.IsNotNull(zipPath);

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var entries = archive.Entries;

                Assert.AreEqual(2, entries.Count);
                Assert.AreEqual(
                    1,
                    entries
                        .Count(x => x.FullName == Path.GetFileName(file1)));
                Assert.AreEqual(
                    1,
                    entries
                        .Count(x => x.FullName == Path.GetFileName(file2)));
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task ProducesProperZipFileForSingleFile()
        {

            var file = Path.GetTempFileName();

            File.WriteAllText(file, "file");

            var fakeDataSourceService = Substitute.For<IDataSourceService>();

            var fileData = new ClipboardFileData(fakeDataSourceService)
            {
                FullPath = file,
                FileName = Path.GetFileName(file)
            };

            var package = new ClipboardDataPackage();
            package.AddData(fileData);

            string zipPath = null;
            container.Resolve<IClipboardInjectionService>()
                .When(x => x.InjectFiles(Arg.Any<string[]>()))
                .Do(
                    parameters => {
                        var files = (string[]) parameters[0];
                        zipPath = files[0];
                    });

            await systemUnderTest.PerformAsync(package);

            Assert.IsNotNull(zipPath);

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var entries = archive.Entries;

                Assert.AreEqual(1, entries.Count);
                Assert.AreEqual(
                    1,
                    entries
                        .Count(x => x.FullName == Path.GetFileName(file)));
            }
        }
    }
}