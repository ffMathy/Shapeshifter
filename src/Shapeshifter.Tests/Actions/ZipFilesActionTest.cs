using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data.Interfaces;
using System.Threading.Tasks;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class ZipFilesActionTest : TestBase
    {
        [TestMethod]
        public async Task CanPerformForFiles()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();
            Assert.IsTrue(await action.CanPerformAsync(Substitute.For<IClipboardFileData>()));
        }

        [TestMethod]
        public async Task CanPerformForFileCollections()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();
            Assert.IsTrue(await action.CanPerformAsync(Substitute.For<IClipboardFileCollectionData>()));
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
