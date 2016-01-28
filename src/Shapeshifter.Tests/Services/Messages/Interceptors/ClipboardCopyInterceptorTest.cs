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
    public class ClipboardCopyInterceptorTest: UnitTestFor<IClipboardCopyInterceptor>
    {
        [TestMethod]
        public void InstallAddsClipboardFormatListener()
        {
            var windowHandle = new IntPtr(1337);

            Container.Resolve<IClipboardNativeApi>()
             .AddClipboardFormatListener(windowHandle)
             .Returns(true);
            
            SystemUnderTest.Install(windowHandle);

            var fakeClipboardNativeApi = Container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi
                .Received()
                .AddClipboardFormatListener(windowHandle);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void InstallThrowsExceptionIfAddingClipboardFormatListenerFailed()
        {
            var windowHandle = new IntPtr(1337);

            Container.Resolve<IClipboardNativeApi>()
             .AddClipboardFormatListener(windowHandle)
             .Returns(false);

            SystemUnderTest.Install(windowHandle);
        }

        [TestMethod]
        public void UninstallRemovesClipboardFormatListener()
        {
            Container.Resolve<IClipboardNativeApi>()
             .RemoveClipboardFormatListener(Arg.Any<IntPtr>())
             .Returns(true);

            SystemUnderTest.Uninstall();

            var fakeClipboardNativeApi = Container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi
                .Received()
                .RemoveClipboardFormatListener(Arg.Any<IntPtr>());
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void UninstallThrowsExceptionIfAddingClipboardFormatListenerFailed()
        {
            var windowHandle = new IntPtr(1337);

            Container.Resolve<IClipboardNativeApi>()
             .AddClipboardFormatListener(windowHandle)
             .Returns(false);
            
            SystemUnderTest.Uninstall();
        }

        [TestMethod]
        public void DataCopiedIsSkippedForEveryNonUniqueSequenceOfCopies()
        {
            var windowHandle = new IntPtr(1337);

            Container.Resolve<IClipboardNativeApi>()
                     .GetClipboardSequenceNumber()
                     .Returns(1u, 1u, 1u);

            var dataCopiedCount = 0;
            
            SystemUnderTest.DataCopied += (sender, e) => dataCopiedCount++;

            SystemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));
            SystemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));
            SystemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));

            Assert.AreEqual(1, dataCopiedCount);
        }

        [TestMethod]
        public void SkipNextSkipsNextItem()
        {
            var windowHandle = new IntPtr(1337);

            Container.Resolve<IClipboardNativeApi>()
                     .GetClipboardSequenceNumber()
                     .Returns(1u, 2u, 3u);

            var dataCopiedCount = 0;
            
            SystemUnderTest.DataCopied += (sender, e) => dataCopiedCount++;

            SystemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));

            Assert.AreEqual(1, dataCopiedCount);

            SystemUnderTest.SkipNext();

            SystemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));

            Assert.AreEqual(1, dataCopiedCount);

            SystemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));

            Assert.AreEqual(2, dataCopiedCount);
        }

        [TestMethod]
        public void DataCopiedIsSkippedForIrrelevantMessages()
        {
            var windowHandle = new IntPtr(1337);

            Container.Resolve<IClipboardNativeApi>()
                     .GetClipboardSequenceNumber()
                     .Returns(1u, 2u, 3u);

            var dataCopiedCount = 0;
            
            SystemUnderTest.DataCopied += (sender, e) => dataCopiedCount++;

            SystemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_HOTKEY,
                    IntPtr.Zero,
                    IntPtr.Zero));
            SystemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));

            Assert.AreEqual(1, dataCopiedCount);
        }

        [TestMethod]
        public void DataCopiedIsTriggeredForEveryUniqueSequenceOfCopies()
        {
            var windowHandle = new IntPtr(1337);

            Container.Resolve<IClipboardNativeApi>()
                     .GetClipboardSequenceNumber()
                     .Returns(1u, 2u, 3u);

            var dataCopiedCount = 0;

            SystemUnderTest.DataCopied += (sender, e) => dataCopiedCount++;

            SystemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));
            SystemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));
            SystemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));

            Assert.AreEqual(3, dataCopiedCount);
        }
    }
}