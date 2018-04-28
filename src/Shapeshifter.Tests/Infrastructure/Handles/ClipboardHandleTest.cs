namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles
{
    using System;
	using System.Linq;
	using Autofac;

    using Controls.Window.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Native.Interfaces;

    using NSubstitute;
	using Shapeshifter.WindowsDesktop.Data.Factories.Interfaces;

	[TestClass]
    public class ClipboardHandleTest: UnitTestFor<IClipboardHandle>
    {
        [TestMethod]
        public void CreatingHandleOpensClipboardWithWindowHandle()
        {
            var fakeClipboardNativeApi = Container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi.Received()
                                  .OpenClipboard(new IntPtr(0));
        }

        [TestMethod]
        public void DisposingHandleClosesClipboard()
        {
            SystemUnderTest.Dispose();

            var fakeClipboardNativeApi = Container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi.Received()
                                  .CloseClipboard();
        }

        [TestMethod]
        public void GetClipboardFormatsForwardsCallToNativeApi()
        {
			ExcludeFakeFor<IClipboardFormatFactory>();

            var fakeFormats = new uint[]
            {
                1337,
				1338
            };
            
            Container.Resolve<IMainWindowHandleContainer>()
             .Handle
             .Returns(new IntPtr(1337));

            Container.Resolve<IClipboardNativeApi>()
                .GetClipboardFormats()
                .Returns(fakeFormats);

            var handle = Container.Resolve<IClipboardHandle>();

            var formats = handle
				.GetClipboardFormats()
				.Select(x => x.Number)
				.ToArray();
            Assert.AreEqual(fakeFormats[0], formats[0]);
            Assert.AreEqual(fakeFormats[1], formats[1]);
		}

        [TestMethod]
        public void SetClipboardDataForwardsCallToNativeApi()
        {
            var fakeClipboardNativeApi = Container.Resolve<IClipboardNativeApi>();
            fakeClipboardNativeApi
                .SetClipboardData(Arg.Any<uint>(), Arg.Any<IntPtr>())
                .Returns(new IntPtr(1338));
            
            var pointer = SystemUnderTest.SetClipboardData(
                1339, new IntPtr(1340));
            Assert.AreEqual(
                new IntPtr(1338), pointer);

            fakeClipboardNativeApi
                .Received()
                .SetClipboardData(1339, new IntPtr(1340));
        }
    }
}