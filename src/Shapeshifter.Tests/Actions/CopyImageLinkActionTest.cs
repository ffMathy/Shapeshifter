using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class CopyImageLinkActionTest : TestBase
    {
        [TestMethod]
        public void CanPerformIsFalseForNonTextTypes()
        {
            var container = CreateContainer();

            var someNonTextData = Substitute.For<IClipboardData>();

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsFalse(action.CanPerform(someNonTextData));
        }

        [TestMethod]
        public void CanPerformIsFalseForTextTypesWithNoImageLink()
        {
            var container = CreateContainer();

            var textDataWithLinkButNoImageLink = Substitute.For<IClipboardTextData>();
            textDataWithLinkButNoImageLink.Text.Returns("hello http://example.com text");

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsFalse(action.CanPerform(textDataWithLinkButNoImageLink));
        }

        [TestMethod]
        public void CanPerformIsTrueForTextTypesWithImageLink()
        {
            var container = CreateContainer();

            var textDataWithImageLink = Substitute.For<IClipboardTextData>();
            textDataWithImageLink.Text.Returns("hello http://example.com/image.png text");

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsTrue(action.CanPerform(textDataWithImageLink));
        }
    }
}
