namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.ViewModels
{
    using Data.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class ClipboardTextDataViewModelTest: UnitTestFor<IClipboardTextDataViewModel>
    {
        [TestMethod]
        public void FriendlyTextRemovesDuplicateWhitespaces()
        {
            var fakeTextData = Substitute.For<IClipboardTextData>();
            fakeTextData.Text.Returns("hello  \t  world");

            systemUnderTest.Data = fakeTextData;

            Assert.AreEqual("hello world", systemUnderTest.FriendlyText);
        }

        [TestMethod]
        public void FriendlyTextDoesNotRemoveSingleWhitespace()
        {
            var fakeTextData = Substitute.For<IClipboardTextData>();
            fakeTextData.Text.Returns("hello world");

            systemUnderTest.Data = fakeTextData;

            Assert.AreEqual("hello world", systemUnderTest.FriendlyText);
        }

        [TestMethod]
        public void FriendlyTextSubstitutesTabCharacters()
        {
            var fakeTextData = Substitute.For<IClipboardTextData>();
            fakeTextData.Text.Returns("hello\tworld");

            systemUnderTest.Data = fakeTextData;

            Assert.AreEqual("hello world", systemUnderTest.FriendlyText);
        }

        [TestMethod]
        public void FriendlyTextIsNotLongerThanHalfKilobyte()
        {
            var fakeTextData = Substitute.For<IClipboardTextData>();

            var repeatText = "hello world";
            while (repeatText.Length < 512)
            {
                repeatText += repeatText;
            }

            fakeTextData.Text.Returns(repeatText);

            systemUnderTest.Data = fakeTextData;

            Assert.AreEqual(512, systemUnderTest.FriendlyText.Length);
        }

        [TestMethod]
        public void FriendlyTextTrimsLeadingWhitespaces()
        {
            var fakeTextData = Substitute.For<IClipboardTextData>();
            fakeTextData.Text.Returns("   hello world   ");

            systemUnderTest.Data = fakeTextData;

            Assert.AreEqual("hello world", systemUnderTest.FriendlyText);
        }
    }
}