using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class CopyLinkActionTest : TestBase
    {
        [TestMethod]
        public void CanPerformIsFalseForNonTextTypes()
        {
            var container = CreateContainer();

            var someNonTextData = Substitute.For<IClipboardData>();

            var action = container.Resolve<IOpenLinkAction>();
            Assert.IsFalse(action.CanPerform(someNonTextData));
        }

        [TestMethod]
        public void CanPerformIsFalseForTextTypesWithNoLink()
        {
            var container = CreateContainer();

            var textDataWithLinkButNoImageLink = Substitute.For<IClipboardTextData>();
            textDataWithLinkButNoImageLink.Text.Returns("hello world");

            var action = container.Resolve<IOpenLinkAction>();
            Assert.IsFalse(action.CanPerform(textDataWithLinkButNoImageLink));
        }

        [TestMethod]
        public void CanPerformIsTrueForTextTypesWithHttpLink()
        {
            var container = CreateContainer();

            var textDataWithImageLink = Substitute.For<IClipboardTextData>();
            textDataWithImageLink.Text.Returns("hello http://example.com text");

            var action = container.Resolve<IOpenLinkAction>();
            Assert.IsTrue(action.CanPerform(textDataWithImageLink));
        }

        [TestMethod]
        public void CanPerformIsTrueForTextTypesWithHttpsLink()
        {
            var container = CreateContainer();

            var textDataWithImageLink = Substitute.For<IClipboardTextData>();
            textDataWithImageLink.Text.Returns("hello https://example.com text");

            var action = container.Resolve<IOpenLinkAction>();
            Assert.IsTrue(action.CanPerform(textDataWithImageLink));
        }
    }
}
