using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.Tests.Actions
{
    [TestClass]
    public class OpenLinkActionTest : TestBase
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
            var container = CreateContainer(c =>
            {
                c.RegisterFake<ILinkParser>()
                    .HasLink(Arg.Any<string>())
                    .Returns(false);
            });

            var textDataWithLinkButNoImageLink = Substitute.For<IClipboardTextData>();

            var action = container.Resolve<IOpenLinkAction>();
            Assert.IsFalse(action.CanPerform(textDataWithLinkButNoImageLink));
        }
    }
}
