using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection.Interfaces;

namespace Shapeshifter.Tests.Controls.Clipboard.ViewModels.Text
{
    [TestClass]
    public class ClipboardFileCollectionDataViewModelTest : TestBase
    {
        [TestMethod]
        public void FileCountReturnsAmountOfFiles()
        {
            var container = CreateContainer();

            var viewModel = container.Resolve<IClipboardFileCollectionDataViewModel>();

            var fakeData = Substitute.For<IClipboardFileCollectionData>();

            var kitten = GenerateFakeFileData("kitten.jpg");
            var house = GenerateFakeFileData("house.jpg");
            var notes = GenerateFakeFileData("notes.docx");

            fakeData.Files.Returns(new[]
            {
                kitten,
                house,
                notes
            });

            viewModel.Data = fakeData;

            Assert.AreEqual(3, viewModel.FileCount);
        }

        private static IClipboardFileData GenerateFakeFileData(string fileName)
        {
            var fakeFileData = Substitute.For<IClipboardFileData>();
            fakeFileData.FileName.Returns(fileName);

            return fakeFileData;
        }
    }
}
