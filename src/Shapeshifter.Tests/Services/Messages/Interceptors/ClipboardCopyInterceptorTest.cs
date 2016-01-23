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

            container.Resolve<IClipboardNativeApi>()
             .AddClipboardFormatListener(windowHandle)
             .Returns(true);
            
            systemUnderTest.Install(windowHandle);

            var fakeClipboardNativeApi = container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi
                .Received()
                .AddClipboardFormatListener(windowHandle);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void InstallThrowsExceptionIfAddingClipboardFormatListenerFailed()
        {
            var windowHandle = new IntPtr(1337);

            container.Resolve<IClipboardNativeApi>()
             .AddClipboardFormatListener(windowHandle)
             .Returns(false);

            systemUnderTest.Install(windowHandle);
        }

        [TestMethod]
        public void UninstallAddsClipboardFormatListener()
        {
            container.Resolve<IClipboardNativeApi>()
             .AddClipboardFormatListener(Arg.Any<IntPtr>())
             .Returns(false);

            systemUnderTest.Uninstall();

            var fakeClipboardNativeApi = container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi
                .Received()
                .RemoveClipboardFormatListener(Arg.Any<IntPtr>());
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void UninstallThrowsExceptionIfAddingClipboardFormatListenerFailed()
        {
            var windowHandle = new IntPtr(1337);

            container.Resolve<IClipboardNativeApi>()
             .AddClipboardFormatListener(windowHandle)
             .Returns(false);
            
            systemUnderTest.Uninstall();
        }

        [TestMethod]
        public void DataCopiedIsSkippedForEveryNonUniqueSequenceOfCopies()
        {
            var windowHandle = new IntPtr(1337);

            container.Resolve<IClipboardNativeApi>()
                     .GetClipboardSequenceNumber()
                     .Returns(1u, 1u, 1u);

            var dataCopiedCount = 0;
            
            systemUnderTest.DataCopied += (sender, e) => dataCopiedCount++;

            systemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));
            systemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));
            systemUnderTest.ReceiveMessageEvent(
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

            container.Resolve<IClipboardNativeApi>()
                     .GetClipboardSequenceNumber()
                     .Returns(1u, 1u, 1u);

            var dataCopiedCount = 0;
            
            systemUnderTest.DataCopied += (sender, e) => dataCopiedCount++;

            systemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));

            Assert.AreEqual(1, dataCopiedCount);

            systemUnderTest.SkipNext();

            systemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));

            Assert.AreEqual(1, dataCopiedCount);

            systemUnderTest.ReceiveMessageEvent(
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

            container.Resolve<IClipboardNativeApi>()
                     .GetClipboardSequenceNumber()
                     .Returns(1u, 2u, 3u);

            var dataCopiedCount = 0;
            
            systemUnderTest.DataCopied += (sender, e) => dataCopiedCount++;

            systemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_HOTKEY,
                    IntPtr.Zero,
                    IntPtr.Zero));
            systemUnderTest.ReceiveMessageEvent(
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

            container.Resolve<IClipboardNativeApi>()
                     .GetClipboardSequenceNumber()
                     .Returns(1u, 2u, 3u);

            var dataCopiedCount = 0;

            systemUnderTest.DataCopied += (sender, e) => dataCopiedCount++;

            systemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));
            systemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));
            systemUnderTest.ReceiveMessageEvent(
                new WindowMessageReceivedArgument(
                    windowHandle,
                    Message.WM_CLIPBOARDUPDATE,
                    IntPtr.Zero,
                    IntPtr.Zero));

            Assert.AreEqual(3, dataCopiedCount);
        }
    }
}