namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors
{
    using System;

    using Autofac;

    using Infrastructure.Events;

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
            var windowHandle = new IntPtr(1337);

            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardNativeApi>()
                     .AddClipboardFormatListener(windowHandle)
                     .Returns(true);
                    c.RegisterFake<IWindowNativeApi>();
                });

            var interceptor = container.Resolve<IClipboardCopyInterceptor>();
            interceptor.Install(windowHandle);

            var fakeClipboardNativeApi = container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi
                .Received()
                .AddClipboardFormatListener(windowHandle);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InstallThrowsExceptionIfAddingClipboardFormatListenerFailed()
        {
            var windowHandle = new IntPtr(1337);

            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardNativeApi>()
                     .AddClipboardFormatListener(windowHandle)
                     .Returns(false);
                    c.RegisterFake<IWindowNativeApi>();
                });

            var interceptor = container.Resolve<IClipboardCopyInterceptor>();
            interceptor.Install(windowHandle);
        }

        [TestMethod]
        public void UninstallAddsClipboardFormatListener()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardNativeApi>()
                     .RemoveClipboardFormatListener(Arg.Any<IntPtr>())
                     .Returns(true);
                    c.RegisterFake<IWindowNativeApi>();
                });

            var interceptor = container.Resolve<IClipboardCopyInterceptor>();
            interceptor.Uninstall();

            var fakeClipboardNativeApi = container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi
                .Received()
                .RemoveClipboardFormatListener(Arg.Any<IntPtr>());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void UninstallThrowsExceptionIfAddingClipboardFormatListenerFailed()
        {
            var windowHandle = new IntPtr(1337);

            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardNativeApi>()
                     .RemoveClipboardFormatListener(windowHandle)
                     .Returns(false);
                    c.RegisterFake<IWindowNativeApi>();
                });

            var interceptor = container.Resolve<IClipboardCopyInterceptor>();
            interceptor.Uninstall();
        }

        [TestMethod]
        public void DataCopiedIsSkippedForEveryNonUniqueSequenceOfCopies()
        {
            var windowHandle = new IntPtr(1337);

            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardNativeApi>()
                        .GetClipboardSequenceNumber()
                        .Returns(1u, 1u, 1u);
                    c.RegisterFake<IWindowNativeApi>();
                });

            var dataCopiedCount = 0;

            var interceptor = container.Resolve<IClipboardCopyInterceptor>();
            interceptor.DataCopied += (sender, e) => dataCopiedCount++;

            interceptor.ReceiveMessageEvent(new WindowMessageReceivedArgument(
                windowHandle, Message.WM_CLIPBOARDUPDATE, IntPtr.Zero, IntPtr.Zero));
            interceptor.ReceiveMessageEvent(new WindowMessageReceivedArgument(
                windowHandle, Message.WM_CLIPBOARDUPDATE, IntPtr.Zero, IntPtr.Zero));
            interceptor.ReceiveMessageEvent(new WindowMessageReceivedArgument(
                windowHandle, Message.WM_CLIPBOARDUPDATE, IntPtr.Zero, IntPtr.Zero));

            Assert.AreEqual(1, dataCopiedCount);
        }

        [TestMethod]
        public void SkipNextSkipsNextItem()
        {
            var windowHandle = new IntPtr(1337);

            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardNativeApi>()
                        .GetClipboardSequenceNumber()
                        .Returns(1u, 2u, 3u);
                    c.RegisterFake<IWindowNativeApi>();
                });

            var dataCopiedCount = 0;

            var interceptor = container.Resolve<IClipboardCopyInterceptor>();
            interceptor.DataCopied += (sender, e) => dataCopiedCount++;

            interceptor.ReceiveMessageEvent(new WindowMessageReceivedArgument(
                windowHandle, Message.WM_CLIPBOARDUPDATE, IntPtr.Zero, IntPtr.Zero));

            Assert.AreEqual(1, dataCopiedCount);

            interceptor.SkipNext();

            interceptor.ReceiveMessageEvent(new WindowMessageReceivedArgument(
                windowHandle, Message.WM_CLIPBOARDUPDATE, IntPtr.Zero, IntPtr.Zero));

            Assert.AreEqual(1, dataCopiedCount);

            interceptor.ReceiveMessageEvent(new WindowMessageReceivedArgument(
                windowHandle, Message.WM_CLIPBOARDUPDATE, IntPtr.Zero, IntPtr.Zero));

            Assert.AreEqual(2, dataCopiedCount);
        }

        [TestMethod]
        public void DataCopiedIsSkippedForIrrelevantMessages()
        {
            var windowHandle = new IntPtr(1337);

            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardNativeApi>()
                        .GetClipboardSequenceNumber()
                        .Returns(1u, 2u, 3u);
                    c.RegisterFake<IWindowNativeApi>();
                });

            var dataCopiedCount = 0;

            var interceptor = container.Resolve<IClipboardCopyInterceptor>();
            interceptor.DataCopied += (sender, e) => dataCopiedCount++;

            interceptor.ReceiveMessageEvent(new WindowMessageReceivedArgument(
                windowHandle, Message.WM_HOTKEY, IntPtr.Zero, IntPtr.Zero));
            interceptor.ReceiveMessageEvent(new WindowMessageReceivedArgument(
                windowHandle, Message.WM_CLIPBOARDUPDATE, IntPtr.Zero, IntPtr.Zero));

            Assert.AreEqual(1, dataCopiedCount);
        }

        [TestMethod]
        public void DataCopiedIsTriggeredForEveryUniqueSequenceOfCopies()
        {
            var windowHandle = new IntPtr(1337);

            var container = CreateContainer(
                c => {
                    c.RegisterFake<IClipboardNativeApi>()
                        .GetClipboardSequenceNumber()
                        .Returns(1u, 2u, 3u);
                    c.RegisterFake<IWindowNativeApi>();
                });

            var dataCopiedCount = 0;

            var interceptor = container.Resolve<IClipboardCopyInterceptor>();
            interceptor.DataCopied += (sender, e) => dataCopiedCount++;

            interceptor.ReceiveMessageEvent(new WindowMessageReceivedArgument(
                windowHandle, Message.WM_CLIPBOARDUPDATE, IntPtr.Zero, IntPtr.Zero));
            interceptor.ReceiveMessageEvent(new WindowMessageReceivedArgument(
                windowHandle, Message.WM_CLIPBOARDUPDATE, IntPtr.Zero, IntPtr.Zero));
            interceptor.ReceiveMessageEvent(new WindowMessageReceivedArgument(
                windowHandle, Message.WM_CLIPBOARDUPDATE, IntPtr.Zero, IntPtr.Zero));

            Assert.AreEqual(3, dataCopiedCount);
        }
    }
}