#region

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Web;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;

#endregion

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class CopyImageLinkActionTest : ActionTestBase
    {
        [TestMethod]
        public async Task CanPerformIsFalseForNonTextTypes()
        {
            var container = CreateContainer();

            var someNonTextData = GetPackageContaining<IClipboardData>();

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsFalse(await action.CanPerformAsync(someNonTextData));
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
        public async Task CanPerformIsFalseForTextTypesWithNoImageLink()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<ILinkParser>()
                    .HasLinkOfTypeAsync(Arg.Any<string>(), LinkType.ImageFile)
                    .Returns(Task.FromResult(false));
            });

            var action = container.Resolve<ICopyImageLinkAction>();
            var canPerform = await action.CanPerformAsync(Substitute.For<IClipboardDataPackage>());
            Assert.IsFalse(canPerform);
        }

        [TestMethod]
        public async Task CanPerformIsTrueForTextTypesWithImageLink()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<ILinkParser>()
                    .HasLinkOfTypeAsync(Arg.Any<string>(), LinkType.ImageFile)
                    .Returns(Task.FromResult(true));
            });

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsTrue(await action.CanPerformAsync(GetPackageContaining<IClipboardTextData>()));
        }

        [TestMethod]
        public async Task PerformTriggersImageDownload()
        {
            var firstFakeDownloadedImageBytes = new byte[] {1};
            var secondFakeDownloadedImageBytes = new byte[] {2};

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
                    .HasLinkOfTypeAsync(Arg.Any<string>(), LinkType.ImageFile)
                    .Returns(Task.FromResult(true));

                c.RegisterFake<ILinkParser>()
                    .ExtractLinksFromTextAsync(Arg.Any<string>())
                    .Returns(Task.FromResult<IEnumerable<string>>(new[] {"foobar.com", "example.com"}));
            });

            var action = container.Resolve<ICopyImageLinkAction>();
            await action.PerformAsync(GetPackageContaining<IClipboardTextData>());

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