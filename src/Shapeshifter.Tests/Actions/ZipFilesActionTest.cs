using System.Threading.Tasks;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class ZipFilesActionTest : ActionTestBase
    {
        [TestMethod]
        public async Task CanPerformForFiles()
        {
            var container = CreateContainer();

            var action = container.Resolve<IZipFilesAction>();
            Assert.IsTrue(await action.CanPerformAsync(GetPackageContaining<IClipboardFileData>()));
        }

        [TestMethod]
        public async Task CanPerformForFileCollections()
        {
            var container = CreateContainer();

            var fakeData = GetPackageContaining<IClipboardFileCollectionData>();

            var action = container.Resolve<IZipFilesAction>();
            Assert.IsTrue(await action.CanPerformAsync(fakeData));
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