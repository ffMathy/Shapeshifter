namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
    using System;
    using System.Windows.Input;

    using Autofac;

    using Factories.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class PasteHotkeyInterceptorTest: UnitTestFor<IPasteHotkeyInterceptor>
    {
        [TestMethod]
        public void IsEnabledByDefault()
        {
            Assert.IsTrue(systemUnderTest.IsEnabled);
        }

        [TestMethod]
        public void RetainsEnabledStateAfterUninstallIfEnabledIsTrue()
        {
            container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Key.V, true, true)
             .Returns(Substitute.For<IHotkeyInterception>());

            systemUnderTest.IsEnabled = true;

            systemUnderTest.Install(IntPtr.Zero);
            systemUnderTest.Uninstall();

            Assert.IsTrue(systemUnderTest.IsEnabled);
        }

        [TestMethod]
        public void RetainsEnabledStateAfterUninstallIfEnabledIsFalse()
        {
            container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Key.V, true, true)
             .Returns(Substitute.For<IHotkeyInterception>());

            systemUnderTest.IsEnabled = false;

            systemUnderTest.Install(IntPtr.Zero);
            systemUnderTest.Uninstall();

            Assert.IsFalse(systemUnderTest.IsEnabled);
        }
    }
}