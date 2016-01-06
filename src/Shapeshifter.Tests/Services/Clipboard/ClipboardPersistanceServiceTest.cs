namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System.Collections.Generic;
    using System.Linq;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    using NSubstitute;

    using WindowsDesktop;

    using Data.Interfaces;
    using System.Threading.Tasks;

    using Data.Factories.Interfaces;

    using Shared.Services.Files.Interfaces;

    [TestClass]
    public class ClipboardPersistanceServiceTest : TestBase
    {
        [TestMethod]
        public async Task CanPersistClipboardData()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IFileManager>()
                        .PrepareNewFolder(Arg.Is<string>(
                            x => x.StartsWith("Pinned")))
                        .Returns("preparedFolder");
                });

            var fakeData1 = Substitute.For<IClipboardData>()
                .WithFakeSettings(
                    x =>
                    {
                        x.RawFormat.Returns(1337u);
                        x.RawData.Returns(
                            new byte[]
                            {
                                1,
                                2
                            });
                    });
            var fakeData2 = Substitute.For<IClipboardData>()
                .WithFakeSettings(
                    x =>
                    {
                        x.RawFormat.Returns(1338u);
                        x.RawData.Returns(
                            new byte[]
                            {
                                3,
                                4
                            });
                    });

            var fakePackage = Substitute.For<IClipboardDataPackage>();
            fakePackage
                .Contents
                .Returns(new List<IClipboardData>(new[]
                {
                    fakeData1,
                    fakeData2
                }));

            var service = container.Resolve<IClipboardPersistanceService>();
            await service.PersistClipboardPackageAsync(fakePackage);

            var fakeFileManager = container.Resolve<IFileManager>();
            fakeFileManager
                .Received()
                .WriteBytesToTemporaryFile(
                    @"preparedFolder\1.1337", fakeData1.RawData);
            fakeFileManager
                .Received()
                .WriteBytesToTemporaryFile(
                    @"preparedFolder\2.1338", fakeData2.RawData);
        }

        [TestCategory("Integration")]
        [TestMethod]
        public async Task CanFetchPersistedPackages()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IClipboardDataFactory>()
                    .WithFakeSettings(
                        x =>
                        {
                            x.CanBuildData(Arg.Any<uint>())
                             .Returns(true);

                            x.BuildData(
                                Arg.Any<uint>(), 
                                Arg.Any<byte[]>())
                                .Returns(
                                    r => {
                                        var resultingData = Substitute.For<IClipboardData>();
                                        resultingData
                                            .RawData
                                            .Returns(
                                                r.Arg<byte[]>());
                                        resultingData
                                            .RawFormat
                                            .Returns(
                                                r.Arg<uint>());
                                        return resultingData;
                                    });
                        });
                });

            var fakeData1 = Substitute.For<IClipboardData>()
                .WithFakeSettings(
                    x =>
                    {
                        x.RawFormat.Returns(1337u);
                        x.RawData.Returns(
                            new byte[]
                            {
                                1,
                                2
                            });
                    });
            var fakeData2 = Substitute.For<IClipboardData>()
                .WithFakeSettings(
                    x =>
                    {
                        x.RawFormat.Returns(1338u);
                        x.RawData.Returns(
                            new byte[]
                            {
                                3,
                                4
                            });
                    });

            var fakePackage1 = Substitute.For<IClipboardDataPackage>();
            fakePackage1
                .Contents
                .Returns(new List<IClipboardData>(new[]
                {
                    fakeData1,
                    fakeData2
                }));

            var fakePackage2 = Substitute.For<IClipboardDataPackage>();
            fakePackage2
                .Contents
                .Returns(new List<IClipboardData>(new[]
                {
                    fakeData2,
                    fakeData1
                }));

            var service = container.Resolve<IClipboardPersistanceService>();
            await service.PersistClipboardPackageAsync(fakePackage1);
            await service.PersistClipboardPackageAsync(fakePackage2);

            var persistedPackages = await service.GetPersistedPackagesAsync();
            var persistedPackagesArray = persistedPackages.ToArray();

            Assert.AreEqual(2, persistedPackagesArray.Length);

            var persistedPackage1 = persistedPackagesArray[0];
            var persistedPackage2 = persistedPackagesArray[1];

            Assert.AreEqual(1, persistedPackage1.Id);
            Assert.AreEqual(2, persistedPackage2.Id);

            Assert.AreEqual(1, 
                persistedPackage1
                    .Contents[0]
                    .RawData[0]);
            Assert.AreEqual(2,
                persistedPackage1
                    .Contents[0]
                    .RawData[1]);
            Assert.AreEqual(3,
                persistedPackage1
                    .Contents[1]
                    .RawData[0]);
            Assert.AreEqual(4,
                persistedPackage1
                    .Contents[1]
                    .RawData[1]);

            Assert.AreEqual(3,
                persistedPackage2
                    .Contents[0]
                    .RawData[0]);
            Assert.AreEqual(4,
                persistedPackage2
                    .Contents[0]
                    .RawData[1]);
            Assert.AreEqual(1,
                persistedPackage2
                    .Contents[1]
                    .RawData[0]);
            Assert.AreEqual(2,
                persistedPackage2
                    .Contents[1]
                    .RawData[1]);
        }
    }
}