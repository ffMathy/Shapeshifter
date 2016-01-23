namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.ViewModels
{
    using System.Linq;

    using Data.Interfaces;

    using FileCollection.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class ClipboardFileCollectionDataViewModelTest: UnitTestFor<IClipboardFileCollectionDataViewModel>
    {
        [TestMethod]
        public void FileCountReturnsAmountOfFiles()
        {
            var fakeData = Substitute.For<IClipboardFileCollectionData>();

            var kitten = GenerateFakeFileData("kitten.jpg");
            var house = GenerateFakeFileData("house.jpg");
            var notes = GenerateFakeFileData("notes.docx");

            fakeData.Files.Returns(
                new[]
                {
                    kitten,
                    house,
                    notes
                });

            systemUnderTest.Data = fakeData;

            Assert.AreEqual(3, systemUnderTest.FileCount);
        }

        [TestMethod]
        public void FileTypeGroupsAreGroupedByFileExtensions()
        {
            var fakeData = Substitute.For<IClipboardFileCollectionData>();

            var notes = GenerateFakeFileData("notes.docx");
            var kitten = GenerateFakeFileData("kitten.jpg");
            var house = GenerateFakeFileData("house.jpg");

            fakeData.Files.Returns(
                new[]
                {
                    notes,
                    kitten,
                    house
                });

            systemUnderTest.Data = fakeData;

            var groups = systemUnderTest.FileTypeGroups.ToArray();
            Assert.AreEqual(2, groups.Length);

            Assert.AreEqual(".jpg", groups[0].FileType);
            Assert.AreEqual(".docx", groups[1].FileType);

            Assert.AreEqual(2, groups[0].Count);
            Assert.AreEqual(1, groups[1].Count);
        }

        static IClipboardFileData GenerateFakeFileData(string fileName)
        {
            var fakeFileData = Substitute.For<IClipboardFileData>();
            fakeFileData.FileName.Returns(fileName);

            return fakeFileData;
        }
    }
}