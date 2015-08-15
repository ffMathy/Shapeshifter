using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class ZipFilesActionTest : TestBase
    {
        [TestMethod]
        public void CanPerformForFiles()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();
            Assert.IsTrue(action.CanPerform(Substitute.For<IClipboardFileData>()));
        }

        [TestMethod]
        public void CanPerformForFileCollections()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();
            Assert.IsTrue(action.CanPerform(Substitute.For<IClipboardFileCollectionData>()));
        }

        [TestMethod]
        public void CanGetDescription()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();
            Assert.IsNotNull(action.Description);
        }

        [TestMethod]
        public void CanGetTitle()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();
            Assert.IsNotNull(action.Title);
        }
    }
}
