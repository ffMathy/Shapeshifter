namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles
{
    using System;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    using NSubstitute;

    using WindowsDesktop;

    using Controls.Window.Interfaces;

    using Native.Interfaces;

    [TestClass]
    public class ClipboardHandleTest: TestBase
    {
        ILifetimeScope GetContainer()
        {
            return CreateContainer(
                c => {
                    c.RegisterFake<IClipboardNativeApi>();
                    c.RegisterFake<IMainWindowHandleContainer>()
                     .Handle
                     .Returns(new IntPtr(1337));
                });
        }

        [TestMethod]
        public void CreatingHandleOpensClipboardWithWindowHandle()
        {
            var container = GetContainer();
            container.Resolve<IClipboardHandle>();

            var fakeClipboardNativeApi = container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi.Received()
                                  .OpenClipboard(new IntPtr(1337));
        }

        [TestMethod]
        public void DisposingHandleClosesClipboard()
        {
            var container = GetContainer();

            var handle = container.Resolve<IClipboardHandle>();
            handle.Dispose();

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

            var container = GetContainer();

            var fakeClipboardNativeApi = container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi
                .GetClipboardFormats()
                .Returns(fakeFormats);

            var handle = container.Resolve<IClipboardHandle>();

            var formats = handle.GetClipboardFormats();
            Assert.AreSame(fakeFormats, formats);
        }

        [TestMethod]
        public void SetClipboardDataForwardsCallToNativeApi()
        {
            var container = GetContainer();

            var fakeClipboardNativeApi = container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi
                .SetClipboardData(Arg.Any<uint>(), Arg.Any<IntPtr>())
                .Returns(new IntPtr(1338));

            var handle = container.Resolve<IClipboardHandle>();

            var pointer = handle.SetClipboardData(1339, new IntPtr(1340));
            Assert.AreEqual(new IntPtr(1338), pointer);

            fakeClipboardNativeApi
                .Received()
                .SetClipboardData(1339, new IntPtr(1340));
        }
    }
}