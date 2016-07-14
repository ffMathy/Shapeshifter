namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System.Threading.Tasks;

    using Autofac;

    using Data.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Files;
    using Services.Files.Interfaces;

    [TestClass]
    public class UploadImageActionTest: ActionTestBase<IUploadImageAction>
    {
        [TestMethod]
        public async Task CanNotPerformWithUnknownDataType()
        {
            var fakeData = Substitute.For<IClipboardDataPackage>();
            Assert.IsFalse(
                await SystemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public void CanReadTitle()
        {
            Assert.IsNotNull(SystemUnderTest.Title);
        }

        [TestMethod]
        public async Task CanReadDescription()
        {
            Assert.IsNotNull(await SystemUnderTest.GetDescriptionAsync(Substitute.For<IClipboardDataPackage>()));
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(50, SystemUnderTest.Order);
        }

        [TestMethod]
        public async Task CanNotPerformWithFileWithNoImage()
        {
            Container.Resolve<IFileTypeInterpreter>()
             .GetFileTypeFromFileName(Arg.Any<string>())
             .Returns(FileType.Other);

            var fakeData = Substitute.For<IClipboardDataPackage>();
            Assert.IsFalse(
                await SystemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public async Task CanPerformWithImageFile()
        {
            Container.Resolve<IFileTypeInterpreter>()
             .GetFileTypeFromFileName(Arg.Any<string>())
             .Returns(FileType.Image);

            var fakeData = GetPackageContaining<IClipboardFileData>();
            Assert.IsTrue(
                await SystemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public async Task CanPerformWithImageData()
        {
            var fakeData = GetPackageContaining<IClipboardImageData>();
            Assert.IsTrue(
                await SystemUnderTest.CanPerformAsync(fakeData));
        }
    }
}