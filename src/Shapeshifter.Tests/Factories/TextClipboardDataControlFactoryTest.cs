using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using NSubstitute;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;

namespace Shapeshifter.Tests.Factories
{
    [TestClass]
    public class TextClipboardDataControlFactoryTest : TestBase
    {
        [TestMethod]
        public void CanBuildDataReturnsTrueForTextFormats()
        {
            var container = CreateContainer();

            var factory = container.Resolve<ITextClipboardDataControlFactory>();
            Assert.IsTrue(factory.CanBuildData("Text"));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForNonTextFormats()
        {
            var container = CreateContainer();

            var factory = container.Resolve<ITextClipboardDataControlFactory>();
            Assert.IsFalse(factory.CanBuildData("foobar"));
        }

        [TestMethod]
        public void CanBuildControlReturnsTrueForTextData()
        {
            var container = CreateContainer();

            var factory = container.Resolve<ITextClipboardDataControlFactory>();
            Assert.IsTrue(factory.CanBuildControl(Substitute.For<IClipboardTextData>()));
        }

        [TestMethod]
        public void CanBuildControlReturnsFalseForNonTextData()
        {
            var container = CreateContainer();

            var factory = container.Resolve<ITextClipboardDataControlFactory>();
            Assert.IsFalse(factory.CanBuildControl(Substitute.For<IClipboardData>()));
        }

        [TestMethod]
        public void BuildDataReturnsTextData()
        {
            var container = CreateContainer();

            var factory = container.Resolve<ITextClipboardDataControlFactory>();
            var data = factory.BuildData("Text", new byte[0]);

            Assert.IsInstanceOfType(data, typeof(ClipboardTextData));
        }

        [TestMethod]
        public void BuildControlReturnsTextControl()
        {
            var fakeTextDataControl = Substitute.For<IClipboardTextDataControl>();
            var fakeTextData = Substitute.For<IClipboardTextData>();

            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardControlFactory<IClipboardTextData, IClipboardTextDataControl>>()
                .CreateControl(fakeTextData)
                .Returns(fakeTextDataControl);
            });

            var factory = container.Resolve<ITextClipboardDataControlFactory>();
            var control = factory.BuildControl(fakeTextData);
            Assert.AreSame(fakeTextDataControl, control);
        }
    }
}
