namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Unwrappers
{
    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Interfaces;

    using Native;

    [TestClass]
    public class GeneralUnwrapperTest: TestBase
    {
        [TestMethod]
        public void CantUnwrapDspBitmaps()
        {
            var container = CreateContainer();

            var unwrapper = container.Resolve<IMemoryUnwrapper>();
            Assert.IsFalse(unwrapper.CanUnwrap(ClipboardNativeApi.CF_DSPBITMAP));
        }

        [TestMethod]
        public void CantUnwrapDspEnhancedMetafile()
        {
            var container = CreateContainer();

            var unwrapper = container.Resolve<IMemoryUnwrapper>();
            Assert.IsFalse(unwrapper.CanUnwrap(ClipboardNativeApi.CF_DSPENHMETAFILE));
        }

        [TestMethod]
        public void CantUnwrapEnhancedMetafile()
        {
            var container = CreateContainer();

            var unwrapper = container.Resolve<IMemoryUnwrapper>();
            Assert.IsFalse(unwrapper.CanUnwrap(ClipboardNativeApi.CF_ENHMETAFILE));
        }

        [TestMethod]
        public void CantUnwrapMetafilePicture()
        {
            var container = CreateContainer();

            var unwrapper = container.Resolve<IMemoryUnwrapper>();
            Assert.IsFalse(unwrapper.CanUnwrap(ClipboardNativeApi.CF_METAFILEPICT));
        }
    }
}