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
    public class ZipFilesActionTest: ActionTestBase
    {
        [TestMethod]
        public async Task CanPerformForFiles()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();
            Assert.IsTrue(await action.CanPerformAsync(GetPackageContaining<IClipboardFileData>()));
        }

        [TestMethod]
        public async Task CanPerformForFileCollections()
        {
            var container = CreateContainer();

            var fakeData = GetPackageContaining<IClipboardFileCollectionData>();

            var action = container.Resolve<IZipFilesAction>();
            Assert.IsTrue(await action.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public void CanGetDescription()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();
            Assert.IsNotNull(action.Description);
        }

        [TestMethod]
        public void CanGetTitle()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();
            Assert.IsNotNull(action.Title);
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();
            Assert.AreEqual(75, action.Order);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        [TestCategory("Integration")]
        public async Task ThrowsExceptionForInvalidData()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();

            var fakeData = Substitute.For<IClipboardData>();

            var package = new ClipboardDataPackage();
            package.AddData(fakeData);

            await action.PerformAsync(package);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        [TestCategory("Integration")]
        public async Task NoFilesAddedThrowsException()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();

            var fakeDataSourceService = Substitute.For<IDataSourceService>();

            var fileCollectionData = new ClipboardFileCollectionData(fakeDataSourceService)
            {
                Files = new Collection<IClipboardFileData>()
            };

            var package = new ClipboardDataPackage();
            package.AddData(fileCollectionData);

            await action.PerformAsync(package);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task ProducesProperZipFileForCollection()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardInjectionService>();
                });

            var fakeClipboardInjectionService = container.Resolve<IClipboardInjectionService>();
            var action = container.Resolve<IZipFilesAction>();

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
            fakeClipboardInjectionService
                .When(x => x.InjectFiles(Arg.Any<string[]>()))
                .Do(
                    parameters => {
                        var files = (string[]) parameters[0];
                        zipPath = files[0];
                    });

            await action.PerformAsync(package);

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
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardInjectionService>();
                });

            var fakeClipboardInjectionService = container.Resolve<IClipboardInjectionService>();
            var action = container.Resolve<IZipFilesAction>();

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
            fakeClipboardInjectionService
                .When(x => x.InjectFiles(Arg.Any<string[]>()))
                .Do(
                    parameters => {
                        var files = (string[]) parameters[0];
                        zipPath = files[0];
                    });

            await action.PerformAsync(package);

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