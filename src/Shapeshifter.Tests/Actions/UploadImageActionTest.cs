using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Data.Interfaces;

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
        public void CanNotPerformWithFileWithNoImageExtension()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardFileData>();
            fakeData.FileName.Returns("kitten.txt");

            var action = container.Resolve<IUploadImageAction>();
            Assert.IsFalse(action.CanPerform(fakeData));
        }

        [TestMethod]
        public void CanPerformWithFileWithJpgExtension()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardFileData>();
            fakeData.FileName.Returns("kitten.jpg");

            var action = container.Resolve<IUploadImageAction>();
            Assert.IsTrue(action.CanPerform(fakeData));
        }

        [TestMethod]
        public void CanPerformWithFileWithPngExtension()
        {
            var container = CreateContainer();

            var fakeData = Substitute.For<IClipboardFileData>();
            fakeData.FileName.Returns("kitten.png");

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
