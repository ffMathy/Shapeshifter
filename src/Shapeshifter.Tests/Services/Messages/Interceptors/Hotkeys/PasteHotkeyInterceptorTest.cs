namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys
{
    using System;
    using System.Windows.Input;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    using NSubstitute;

    using WindowsDesktop;
    using Factories.Interfaces;

    using Native;

    [TestClass]
    public class PasteHotkeyInterceptorTest: TestBase
    {
        [TestMethod]
        public void IsEnabledByDefault()
        {
            var container = CreateContainer();

            var interceptor = container.Resolve<IPasteHotkeyInterceptor>();
            Assert.IsTrue(interceptor.IsEnabled);
        }
        
        [TestMethod]
        public void RetainsEnabledStateAfterUninstallIfEnabledIsTrue()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IHotkeyInterceptionFactory>()
                     .CreateInterception(Key.V, true, true)
                     .Returns(Substitute.For<IHotkeyInterception>());
                });

            var interceptor = container.Resolve<IPasteHotkeyInterceptor>();
            interceptor.IsEnabled = true;

            interceptor.Install(IntPtr.Zero);
            interceptor.Uninstall();

            Assert.IsTrue(interceptor.IsEnabled);
        }

        [TestMethod]
        public void RetainsEnabledStateAfterUninstallIfEnabledIsFalse()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IHotkeyInterceptionFactory>()
                     .CreateInterception(Key.V, true, true)
                     .Returns(Substitute.For<IHotkeyInterception>());
                });

            var interceptor = container.Resolve<IPasteHotkeyInterceptor>();
            interceptor.IsEnabled = false;

            interceptor.Install(IntPtr.Zero);
            interceptor.Uninstall();

            Assert.IsFalse(interceptor.IsEnabled);
        }
    }
}