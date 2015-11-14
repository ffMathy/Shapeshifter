using System.Linq;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.FileCollection.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.Tests.Controls.Clipboard.ViewModels
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

        [TestMethod]
        public void FileTypeGroupsAreGroupedByFileExtensions()
        {
            var container = CreateContainer();

            var viewModel = container.Resolve<IClipboardFileCollectionDataViewModel>();

            var fakeData = Substitute.For<IClipboardFileCollectionData>();

            var notes = GenerateFakeFileData("notes.docx");
            var kitten = GenerateFakeFileData("kitten.jpg");
            var house = GenerateFakeFileData("house.jpg");

            fakeData.Files.Returns(new[]
            {
                notes,
                kitten,
                house
            });

            viewModel.Data = fakeData;

            var groups = viewModel.FileTypeGroups.ToArray();
            Assert.AreEqual(2, groups.Length);

            Assert.AreEqual(".jpg", groups[0].FileType);
            Assert.AreEqual(".docx", groups[1].FileType);

            Assert.AreEqual(2, groups[0].Count);
            Assert.AreEqual(1, groups[1].Count);
        }

        private static IClipboardFileData GenerateFakeFileData(string fileName)
        {
            var fakeFileData = Substitute.For<IClipboardFileData>();
            fakeFileData.FileName.Returns(fileName);

            return fakeFileData;
        }
    }
}