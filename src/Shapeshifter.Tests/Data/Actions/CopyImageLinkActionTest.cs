namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;

    using Autofac;

    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Clipboard.Interfaces;
    using Services.Images.Interfaces;
    using Services.Web;
    using Services.Web.Interfaces;

    [TestClass]
    public class CopyImageLinkActionTest: ActionTestBase<ICopyImageLinkAction>
    {
        [TestMethod]
        public async Task CanPerformIsFalseForNonTextTypes()
        {
            var someNonTextData = GetPackageContaining<IClipboardData>();

            Assert.IsFalse(await SystemUnderTest.CanPerformAsync(someNonTextData));
        }

        [TestMethod]
        public void CanReadTitle()
        {
            Assert.IsNotNull(SystemUnderTest.Title);
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(100, SystemUnderTest.Order);
        }

        [TestMethod]
        public void CanReadDescription()
        {
            Assert.IsNotNull(SystemUnderTest.Description);
        }

        [TestMethod]
        public async Task CanPerformIsFalseForTextTypesWithNoImageLink()
        {
            Container.Resolve<ILinkParser>()
                .HasLinkOfTypeAsync(
                    Arg.Any<string>(),
                    LinkType.ImageFile)
                .Returns(Task.FromResult(false));
            
            var canPerform = await SystemUnderTest.CanPerformAsync(Substitute.For<IClipboardDataPackage>());
            Assert.IsFalse(canPerform);
        }

        [TestMethod]
        public async Task CanPerformIsTrueForTextTypesWithImageLink()
        {
            Container.Resolve<ILinkParser>()
                .HasLinkOfTypeAsync(
                    Arg.Any<string>(),
                    LinkType.ImageFile)
                .Returns(Task.FromResult(true));
            
            Assert.IsTrue(await SystemUnderTest.CanPerformAsync(GetPackageContaining<IClipboardTextData>()));
        }

        [TestMethod]
        public async Task PerformTriggersImageDownload()
        {
            var firstFakeDownloadedImageBytes = new byte[]
            {
                1
            };
            var secondFakeDownloadedImageBytes = new byte[]
            {
                2
            };
            
            Container.Resolve<IDownloader>()
             .DownloadBytesAsync(Arg.Any<string>())
             .Returns(
                 Task.FromResult(
                     firstFakeDownloadedImageBytes),
                 Task.FromResult(
                     secondFakeDownloadedImageBytes));

            Container.Resolve<ILinkParser>()
             .HasLinkOfTypeAsync(
                 Arg.Any<string>(),
                 LinkType.ImageFile)
             .Returns(Task.FromResult(true));

            Container.Resolve<ILinkParser>()
             .ExtractLinksFromTextAsync(Arg.Any<string>())
             .Returns(
                 Task
                     .FromResult
                     <IReadOnlyCollection<string>>(
                         new[]
                         {
                                     "foobar.com",
                                     "example.com"
                         }));
            
            await SystemUnderTest.PerformAsync(GetPackageContaining<IClipboardTextData>());

            var fakeClipboardInjectionService = Container.Resolve<IClipboardInjectionService>();
            fakeClipboardInjectionService.Received(2)
                                         .InjectImage(Arg.Any<BitmapSource>());

            var fakeImageFileInterpreter = Container.Resolve<IImageFileInterpreter>();
            fakeImageFileInterpreter.Received(1)
                                    .Interpret(firstFakeDownloadedImageBytes);
            fakeImageFileInterpreter.Received(1)
                                    .Interpret(secondFakeDownloadedImageBytes);

            var fakeDownloader = Container.Resolve<IDownloader>();
            fakeDownloader.Received(1)
                          .DownloadBytesAsync("foobar.com")
                          .IgnoreAwait();
            fakeDownloader.Received(1)
                          .DownloadBytesAsync("example.com")
                          .IgnoreAwait();
        }
    }
}