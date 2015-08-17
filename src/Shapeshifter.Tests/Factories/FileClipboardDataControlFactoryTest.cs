using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using System;
using Shapeshifter.Core.Data;

namespace Shapeshifter.Tests.Factories
{
    [TestClass]
    public class FileClipboardDataControlFactoryTest : TestBase
    {
        [TestMethod]
        public void CanBuildDataReturnsTrueForSingleFileFormats()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IFileClipboardDataControlFactory>();
            Assert.IsTrue(factory.CanBuildData("FileDrop"));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForNonFileFormats()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IFileClipboardDataControlFactory>();
            Assert.IsFalse(factory.CanBuildData("foobar"));
        }

        [TestMethod]
        public void BuildsFileControlForSingleFileData()
        {
            var fakeSingleFileData = Substitute.For<IClipboardFileData>();
            var fakeControl = Substitute.For<IClipboardFileDataControl>();

            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardControlFactory<IClipboardFileData, IClipboardFileDataControl>>()
                .CreateControl(fakeSingleFileData)
                .Returns(fakeControl);
            });

            var factory = container.Resolve<IFileClipboardDataControlFactory>();
            var control = factory.BuildControl(fakeSingleFileData);

            Assert.AreSame(fakeControl, control);
        }

        [TestMethod]
        public void BuildsFileControlForMultipleFileData()
        {
            var fakeSingleFileData = Substitute.For<IClipboardFileCollectionData>();
            var fakeControl = Substitute.For<IClipboardFileCollectionDataControl>();

            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardControlFactory<IClipboardFileCollectionData, IClipboardFileCollectionDataControl>>()
                .CreateControl(fakeSingleFileData)
                .Returns(fakeControl);
            });

            var factory = container.Resolve<IFileClipboardDataControlFactory>();
            var control = factory.BuildControl(fakeSingleFileData);

            Assert.AreSame(fakeControl, control);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BuildControlForUnknownTypeThrowsException()
        {
            var fakeData = Substitute.For<IClipboardData>();

            var container = CreateContainer();

            var factory = container.Resolve<IFileClipboardDataControlFactory>();
            factory.BuildControl(fakeData);
        }
    }
}
