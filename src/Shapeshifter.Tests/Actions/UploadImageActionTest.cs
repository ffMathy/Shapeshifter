using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class UploadImageActionTest : TestBase
    {
        [TestMethod]
        public void CanNotPerformWithUnknownDataType()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardData>();

            var action = container.Resolve<IUploadImageAction>();
            Assert.IsFalse(action.CanPerform(fakeData));
        }

        [TestMethod]
        public void CanNotPerformWithFileWithNoImage()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IFileTypeInterpreter>()
                    .GetFileTypeFromFileName(Arg.Any<string>())
                    .Returns(FileType.Other);
            });

            var fakeData = Substitute.For<IClipboardFileData>();

            var action = container.Resolve<IUploadImageAction>();
            Assert.IsFalse(action.CanPerform(fakeData));
        }

        [TestMethod]
        public void CanPerformWithImageFile()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IFileTypeInterpreter>()
                    .GetFileTypeFromFileName(Arg.Any<string>())
                    .Returns(FileType.Image);
            });

            var fakeData = Substitute.For<IClipboardFileData>();

            var action = container.Resolve<IUploadImageAction>();
            Assert.IsTrue(action.CanPerform(fakeData));
        }

        [TestMethod]
        public void CanPerformWithImageData()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardImageData>();

            var action = container.Resolve<IUploadImageAction>();
            Assert.IsTrue(action.CanPerform(fakeData));
        }
    }
}
