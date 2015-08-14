using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class CopyImageLinkActionTest : TestBase
    {
        [TestMethod]
        public void CanPerformIsFalseForNonTextTypes()
        {
            var container = CreateContainer();

            var someNonTextData = Substitute.For<IClipboardData>();

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsFalse(action.CanPerform(someNonTextData));
        }

        [TestMethod]
        public void CanReadTitle()
        {
            var container = CreateContainer();

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsNotNull(action.Title);
        }

        [TestMethod]
        public void CanReadDescription()
        {
            var container = CreateContainer();

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsNotNull(action.Description);
        }

        [TestMethod]
        public void CanPerformIsFalseForTextTypesWithNoImageLink()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<ILinkParser>()
                .HasLinkOfType(Arg.Any<string>(), LinkType.ImageFile).Returns(false);
            });

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsFalse(action.CanPerform(Substitute.For<IClipboardTextData>()));
        }

        [TestMethod]
        public void CanPerformIsTrueForTextTypesWithImageLink()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<ILinkParser>()
                .HasLinkOfType(Arg.Any<string>(), LinkType.ImageFile).Returns(true);
            });

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsTrue(action.CanPerform(Substitute.For<IClipboardTextData>()));
        }

        [TestMethod]
        public async Task PerformTriggersImageDownload()
        {
            var firstFakeDownloadedImageBytes = new byte[] { 1 };
            var secondFakeDownloadedImageBytes = new byte[] { 2 };

            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardInjectionService>();

                c.RegisterFake<IImageFileInterpreter>();

                c.RegisterFake<IDownloader>()
                    .DownloadBytesAsync(Arg.Any<string>())
                    .Returns(
                        Task.FromResult(firstFakeDownloadedImageBytes),
                        Task.FromResult(secondFakeDownloadedImageBytes));

                c.RegisterFake<ILinkParser>()
                    .ExtractLinksFromText(Arg.Any<string>())
                    .Returns(new[] { "foobar.com", "example.com" });
            });

            var action = container.Resolve<ICopyImageLinkAction>();
            await action.PerformAsync(Substitute.For<IClipboardTextData>());

            var fakeClipboardInjectionService = container.Resolve<IClipboardInjectionService>();
            fakeClipboardInjectionService.Received(2)
                .InjectImage(Arg.Any<BitmapSource>());

            var fakeImageFileInterpreter = container.Resolve<IImageFileInterpreter>();
            fakeImageFileInterpreter.Received(1)
                .Interpret(firstFakeDownloadedImageBytes);
            fakeImageFileInterpreter.Received(1)
                .Interpret(secondFakeDownloadedImageBytes);

            var fakeDownloader = container.Resolve<IDownloader>();
            fakeDownloader.Received(1)
                .DownloadBytesAsync("foobar.com")
                .IgnoreAwait();
            fakeDownloader.Received(1)
                .DownloadBytesAsync("example.com")
                .IgnoreAwait();
        }
    }
}
