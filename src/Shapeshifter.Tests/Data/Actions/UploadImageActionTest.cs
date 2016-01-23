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
                await systemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public void CanReadTitle()
        {
            Assert.IsNotNull(systemUnderTest.Title);
        }

        [TestMethod]
        public void CanReadDescription()
        {
            Assert.IsNotNull(systemUnderTest.Description);
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(50, systemUnderTest.Order);
        }

        [TestMethod]
        public async Task CanNotPerformWithFileWithNoImage()
        {
            container.Resolve<IFileTypeInterpreter>()
             .GetFileTypeFromFileName(Arg.Any<string>())
             .Returns(FileType.Other);

            var fakeData = Substitute.For<IClipboardDataPackage>();
            Assert.IsFalse(
                await systemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public async Task CanPerformWithImageFile()
        {
            container.Resolve<IFileTypeInterpreter>()
             .GetFileTypeFromFileName(Arg.Any<string>())
             .Returns(FileType.Image);

            var fakeData = GetPackageContaining<IClipboardFileData>();
            Assert.IsTrue(
                await systemUnderTest.CanPerformAsync(fakeData));
        }

        [TestMethod]
        public async Task CanPerformWithImageData()
        {
            var fakeData = GetPackageContaining<IClipboardImageData>();
            Assert.IsTrue(
                await systemUnderTest.CanPerformAsync(fakeData));
        }
    }
}