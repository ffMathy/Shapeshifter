namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Autofac;

    using Data.Factories.Interfaces;
    using Data.Interfaces;

    using Files.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class ClipboardPersistanceServiceTest: UnitTestFor<IClipboardPersistanceService>
    {

        [TestMethod]
        public async Task CanPersistClipboardData()
        {
            Container.Resolve<IFileManager>()
             .PrepareNewIsolatedFolder(
                 Arg.Is<string>(
                     x => x.StartsWith("Pinned")))
             .Returns("preparedFolder");

            var fakeData1 = Substitute.For<IClipboardData>()
                                      .With(
                                          x => {
                                              x.RawFormat.Returns(1337u);
                                              x.RawData.Returns(
                                                  new byte[]
                                                  {
                                                      1,
                                                      2
                                                  });
                                          });
            var fakeData2 = Substitute.For<IClipboardData>()
                                      .With(
                                          x => {
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
                .Returns(
                    new List<IClipboardData>(
                        new[]
                        {
                            fakeData1,
                            fakeData2
                        }));

            var service = Container.Resolve<IClipboardPersistanceService>();
            await service.PersistClipboardPackageAsync(fakePackage);

            var fakeFileManager = Container.Resolve<IFileManager>();
            fakeFileManager
                .Received()
                .WriteBytesToFileAsync(
                    @"preparedFolder\1.1337",
                    fakeData1.RawData)
				.IgnoreAwait();
            fakeFileManager
                .Received()
                .WriteBytesToFileAsync(
                    @"preparedFolder\2.1338",
                    fakeData2.RawData)
				.IgnoreAwait();
        }

        [TestCategory("Integration")]
        [TestMethod]
        public async Task CanFetchPersistedPackages()
        {
            IncludeFakeFor<IClipboardDataFactory>();

            ExcludeFakeFor<IClipboardDataPackageFactory>();
            ExcludeFakeFor<IFileManager>();

            Container.Resolve<IClipboardDataFactory>()
             .With(
                 x => {
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

            var fakeData1 = Substitute.For<IClipboardData>()
                                      .With(
                                          x => {
                                              x.RawFormat.Returns(1337u);
                                              x.RawData.Returns(
                                                  new byte[]
                                                  {
                                                      1,
                                                      2
                                                  });
                                          });
            var fakeData2 = Substitute.For<IClipboardData>()
                                      .With(
                                          x => {
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
                .Returns(
                    new List<IClipboardData>(
                        new[]
                        {
                            fakeData1,
                            fakeData2
                        }));

            var fakePackage2 = Substitute.For<IClipboardDataPackage>();
            fakePackage2
                .Contents
                .Returns(
                    new List<IClipboardData>(
                        new[]
                        {
                            fakeData2,
                            fakeData1
                        }));
            
            await SystemUnderTest.PersistClipboardPackageAsync(fakePackage1);
            await SystemUnderTest.PersistClipboardPackageAsync(fakePackage2);

            var persistedPackages = await SystemUnderTest.GetPersistedPackagesAsync();
            var persistedPackagesArray = persistedPackages.ToArray();

            Assert.AreEqual(2, persistedPackagesArray.Length);

            var persistedPackage1 = persistedPackagesArray[0];
            var persistedPackage2 = persistedPackagesArray[1];

            Assert.AreEqual(1, persistedPackage1.Id);
            Assert.AreEqual(2, persistedPackage2.Id);

            Assert.AreEqual(
                1,
                persistedPackage1
                    .Contents[0]
                    .RawData[0]);
            Assert.AreEqual(
                2,
                persistedPackage1
                    .Contents[0]
                    .RawData[1]);
            Assert.AreEqual(
                3,
                persistedPackage1
                    .Contents[1]
                    .RawData[0]);
            Assert.AreEqual(
                4,
                persistedPackage1
                    .Contents[1]
                    .RawData[1]);

            Assert.AreEqual(
                3,
                persistedPackage2
                    .Contents[0]
                    .RawData[0]);
            Assert.AreEqual(
                4,
                persistedPackage2
                    .Contents[0]
                    .RawData[1]);
            Assert.AreEqual(
                1,
                persistedPackage2
                    .Contents[1]
                    .RawData[0]);
            Assert.AreEqual(
                2,
                persistedPackage2
                    .Contents[1]
                    .RawData[1]);
        }
    }
}