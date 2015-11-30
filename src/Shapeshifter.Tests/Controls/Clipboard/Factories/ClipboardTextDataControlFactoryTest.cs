namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;

    using Autofac;

    using Clipboard.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Interfaces;
    using Data.Interfaces;

    [TestClass]
    public class ClipboardTextDataControlFactoryTest: TestBase
    {
        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void CreateControlWithNoDataThrowsException()
        {
            var container = CreateContainer();

            var factory =
                container
                    .Resolve
                    <IClipboardDataControlFactory>();
            factory.BuildControl(null);
        }

        [TestMethod]
        public void CanBuildControlReturnsTrueForTextData()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IClipboardDataControlFactory>();
            Assert.IsTrue(factory.CanBuildControl(Substitute.For<IClipboardTextData>()));
        }

        [TestMethod]
        public void CanBuildControlReturnsFalseForNonTextData()
        {
            var container = CreateContainer();

            var factory = container.Resolve<IClipboardDataControlFactory>();
            Assert.IsFalse(factory.CanBuildControl(Substitute.For<IClipboardData>()));
        }

        [TestMethod]
        public void BuildControlReturnsTextControl()
        {
            var fakeTextDataControl = Substitute.For<IClipboardControl>();
            var fakeTextData = Substitute.For<IClipboardTextData>();

            var container = CreateContainer(
                c =>
                {
                    c
                        .RegisterFake<IClipboardDataControlFactory>()
                        .BuildControl(fakeTextData)
                        .Returns(fakeTextDataControl);
                });

            var factory = container.Resolve<IClipboardDataControlFactory>();
            var control = factory.BuildControl(fakeTextData);
            Assert.AreSame(fakeTextDataControl, control);
        }
    }
}