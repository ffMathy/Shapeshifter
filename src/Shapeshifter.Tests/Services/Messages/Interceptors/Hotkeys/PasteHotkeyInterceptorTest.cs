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
            Assert.IsTrue(SystemUnderTest.IsEnabled);
        }

        [TestMethod]
        public void RetainsEnabledStateAfterUninstallIfEnabledIsTrue()
        {
            Container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Key.V, true, true)
             .Returns(Substitute.For<IHotkeyInterception>());

            SystemUnderTest.IsEnabled = true;

            SystemUnderTest.Install(IntPtr.Zero);
            SystemUnderTest.Uninstall();

            Assert.IsTrue(SystemUnderTest.IsEnabled);
        }

        [TestMethod]
        public void RetainsEnabledStateAfterUninstallIfEnabledIsFalse()
        {
            Container.Resolve<IHotkeyInterceptionFactory>()
             .CreateInterception(Key.V, true, true)
             .Returns(Substitute.For<IHotkeyInterception>());

            SystemUnderTest.IsEnabled = false;

            SystemUnderTest.Install(IntPtr.Zero);
            SystemUnderTest.Uninstall();

            Assert.IsFalse(SystemUnderTest.IsEnabled);
        }
    }
}