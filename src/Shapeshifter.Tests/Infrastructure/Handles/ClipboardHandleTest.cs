namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles
{
    using System;

    using Autofac;

    using Controls.Window.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Native.Interfaces;

    using NSubstitute;

    [TestClass]
    public class ClipboardHandleTest: UnitTestFor<IClipboardHandle>
    {

        [TestMethod]
        public void CreatingHandleOpensClipboardWithWindowHandle()
        {
            container.Resolve<IMainWindowHandleContainer>()
             .Handle
             .Returns(new IntPtr(1337));

            var fakeClipboardNativeApi = container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi.Received()
                                  .OpenClipboard(new IntPtr(1337));
        }

        [TestMethod]
        public void DisposingHandleClosesClipboard()
        {
            container.Resolve<IMainWindowHandleContainer>()
             .Handle
             .Returns(new IntPtr(1337));
            
            systemUnderTest.Dispose();

            var fakeClipboardNativeApi = container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi.Received()
                                  .CloseClipboard();
        }

        [TestMethod]
        public void GetClipboardFormatsForwardsCallToNativeApi()
        {
            var fakeFormats = new uint[]
            {
                1337,
                1338
            };
            
            container.Resolve<IMainWindowHandleContainer>()
             .Handle
             .Returns(new IntPtr(1337));

            container.Resolve<IClipboardNativeApi>()
                .GetClipboardFormats()
                .Returns(fakeFormats);

            var handle = container.Resolve<IClipboardHandle>();

            var formats = handle.GetClipboardFormats();
            Assert.AreSame(fakeFormats, formats);
        }

        [TestMethod]
        public void SetClipboardDataForwardsCallToNativeApi()
        {
            var fakeClipboardNativeApi = container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi
                .SetClipboardData(Arg.Any<uint>(), Arg.Any<IntPtr>())
                .Returns(new IntPtr(1338));
            
            var pointer = systemUnderTest.SetClipboardData(
                1339, new IntPtr(1340));
            Assert.AreEqual(
                new IntPtr(1338), pointer);

            fakeClipboardNativeApi
                .Received()
                .SetClipboardData(1339, new IntPtr(1340));
        }
    }
}