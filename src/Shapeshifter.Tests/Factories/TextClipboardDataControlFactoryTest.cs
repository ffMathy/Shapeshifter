using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using NSubstitute;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

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
            Assert.IsTrue(factory.CanBuildData(ClipboardApi.CF_TEXT));
        }

        [TestMethod]
        public void CanBuildDataReturnsFalseForNonTextFormats()
        {
            var container = CreateContainer();

            var factory = container.Resolve<ITextClipboardDataControlFactory>();
            Assert.IsFalse(factory.CanBuildData(uint.MaxValue));
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
            var data = factory.BuildData(ClipboardApi.CF_TEXT, new byte[0]);

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
