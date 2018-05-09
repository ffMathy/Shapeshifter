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
        public async Task CanReadDescription()
        {
            Assert.IsNotNull(await SystemUnderTest.GetTitleAsync(Substitute.For<IClipboardDataPackage>()));
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
        [Ignore]
        public async Task CanPerformWithImageFile()
        {
            Container.Resolve<IFileTypeInterpreter>()
             .GetFileTypeFromFileName(Arg.Any<string>())
             .Returns(FileType.Image);

            var fakeData = CreateClipboardDataPackageContaining<IClipboardFileData>();
            Assert.IsTrue(
                await SystemUnderTest.CanPerformAsync(fakeData));
        }

        [Ignore]
        [TestMethod]
        public async Task CanPerformWithImageData()
        {
            var fakeData = CreateClipboardDataPackageContaining<IClipboardImageData>();
            Assert.IsTrue(
                await SystemUnderTest.CanPerformAsync(fakeData));
        }
    }
}