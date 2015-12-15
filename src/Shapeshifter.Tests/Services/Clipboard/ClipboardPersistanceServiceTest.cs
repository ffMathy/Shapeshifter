namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System.Collections.Generic;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    using NSubstitute;

    using WindowsDesktop;

    using Data.Interfaces;
    using System.Threading.Tasks;

    using Files.Interfaces;

    [TestClass]
    public class ClipboardPersistanceServiceTest: TestBase
    {
        [TestMethod]
        public async Task CanPersistClipboardData()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IFileManager>()
                        .PrepareFolder(Arg.Is<string>(
                            x => x.StartsWith("Pinned")))
                        .Returns("preparedFolder");
                });

            var fakeData1 = Substitute.For<IClipboardData>()
                .WithFakeSettings(
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
                .WithFakeSettings(
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
                .Returns(new List<IClipboardData>(new []
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
    }
}