namespace Shapeshifter.Tests.Actions
{
    using System.Threading.Tasks;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using UserInterface.WindowsDesktop.Actions.Interfaces;
    using UserInterface.WindowsDesktop.Data.Interfaces;
    using UserInterface.WindowsDesktop.Services.Files;
    using UserInterface.WindowsDesktop.Services.Files.Interfaces;

    [TestClass]
    public class UploadImageActionTest: ActionTestBase
    {
        [TestMethod]
        public async Task CanNotPerformWithUnknownDataType()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardDataPackage>();

            var action = container.Resolve<IUploadImageAction>();
            Assert.IsFalse(await action.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public void CanReadTitle()
        {
            var container = CreateContainer();

            var action = container.Resolve<IUploadImageAction>();
            Assert.IsNotNull(action.Title);
        }

        [TestMethod]
        public void CanReadDescription()
        {
            var container = CreateContainer();

            var action = container.Resolve<IUploadImageAction>();
            Assert.IsNotNull(action.Description);
        }

        [TestMethod]
        public async Task CanNotPerformWithFileWithNoImage()
        {
            var container = CreateContainer(
                                            c =>
                                            {
                                                c.RegisterFake<IFileTypeInterpreter>()
                                                 .GetFileTypeFromFileName(Arg.Any<string>())
                                                 .Returns(FileType.Other);
                                            });

            var fakeData = Substitute.For<IClipboardDataPackage>();

            var action = container.Resolve<IUploadImageAction>();
            Assert.IsFalse(await action.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public async Task CanPerformWithImageFile()
        {
            var container = CreateContainer(
                                            c =>
                                            {
                                                c.RegisterFake<IFileTypeInterpreter>()
                                                 .GetFileTypeFromFileName(Arg.Any<string>())
                                                 .Returns(FileType.Image);
                                            });

            var fakeData = GetPackageContaining<IClipboardFileData>();

            var action = container.Resolve<IUploadImageAction>();
            Assert.IsTrue(await action.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public async Task CanPerformWithImageData()
        {
            var container = CreateContainer();

            var fakeData = GetPackageContaining<IClipboardImageData>();

            var action = container.Resolve<IUploadImageAction>();
            Assert.IsTrue(await action.CanPerformAsync(fakeData));
        }
    }
}