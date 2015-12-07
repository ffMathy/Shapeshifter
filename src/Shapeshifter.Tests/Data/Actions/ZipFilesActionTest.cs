namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Actions
{
    using System.Threading.Tasks;

    using Autofac;

    using Data.Actions.Interfaces;
    using Data.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ZipFilesActionTest: ActionTestBase
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