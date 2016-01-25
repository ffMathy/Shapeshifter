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

            SystemUnderTest.Data = fakeTextData;

            Assert.AreEqual("hello world", SystemUnderTest.FriendlyText);
        }

        [TestMethod]
        public void FriendlyTextDoesNotRemoveSingleWhitespace()
        {
            var fakeTextData = Substitute.For<IClipboardTextData>();
            fakeTextData.Text.Returns("hello world");

            SystemUnderTest.Data = fakeTextData;

            Assert.AreEqual("hello world", SystemUnderTest.FriendlyText);
        }

        [TestMethod]
        public void FriendlyTextSubstitutesTabCharacters()
        {
            var fakeTextData = Substitute.For<IClipboardTextData>();
            fakeTextData.Text.Returns("hello\tworld");

            SystemUnderTest.Data = fakeTextData;

            Assert.AreEqual("hello world", SystemUnderTest.FriendlyText);
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

            SystemUnderTest.Data = fakeTextData;

            Assert.AreEqual(512, SystemUnderTest.FriendlyText.Length);
        }

        [TestMethod]
        public void FriendlyTextTrimsLeadingWhitespaces()
        {
            var fakeTextData = Substitute.For<IClipboardTextData>();
            fakeTextData.Text.Returns("   hello world   ");

            SystemUnderTest.Data = fakeTextData;

            Assert.AreEqual("hello world", SystemUnderTest.FriendlyText);
        }
    }
}