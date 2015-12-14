namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors
{
    using System;

    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Native.Interfaces;

    using NSubstitute;

    [TestClass]
    public class ClipboardCopyInterceptorTest: TestBase
    {
        [TestMethod]
        public void InstallAddsClipboardFormatListener()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardNativeApi>()
                     .AddClipboardFormatListener(new IntPtr(1337))
                     .Returns(true);
                    c.RegisterFake<IWindowNativeApi>();
                });

            var interceptor = container.Resolve<IClipboardCopyInterceptor>();
            interceptor.Install(new IntPtr(1337));

            var fakeClipboardNativeApi = container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi
                .Received()
                .AddClipboardFormatListener(new IntPtr(1337));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InstallThrowsExceptionIfAddingClipboardFormatListenerFailed()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardNativeApi>()
                     .AddClipboardFormatListener(new IntPtr(1337))
                     .Returns(false);
                    c.RegisterFake<IWindowNativeApi>();
                });

            var interceptor = container.Resolve<IClipboardCopyInterceptor>();
            interceptor.Install(new IntPtr(1337));
        }
    }
}