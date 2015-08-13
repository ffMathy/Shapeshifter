using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services;
using System;

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
        public void CanReadTitle()
        {
            var container = CreateContainer();

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsNotNull(action.Title);
        }

        [TestMethod]
        public void CanReadDescription()
        {
            var container = CreateContainer();

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsNotNull(action.Description);
        }

        [TestMethod]
        public void CanPerformIsFalseForTextTypesWithNoImageLink()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<ILinkParser>()
                .HasLinkOfType(Arg.Any<string>(), LinkType.ImageFile).Returns(false);
            });
            
            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsFalse(action.CanPerform(Substitute.For<IClipboardTextData>()));
        }

        [TestMethod]
        public void CanPerformIsTrueForTextTypesWithImageLink()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<ILinkParser>()
                .HasLinkOfType(Arg.Any<string>(), LinkType.ImageFile).Returns(true);
            });

            var action = container.Resolve<ICopyImageLinkAction>();
            Assert.IsTrue(action.CanPerform(Substitute.For<IClipboardTextData>()));
        }

        [TestMethod]
        public void PerformWithTextDataCopiesImageFromUrlToClipboard()
        {
            throw new NotImplementedException();
        }
    }
}
