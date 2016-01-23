namespace Shapeshifter.WindowsDesktop.Data.Unwrappers
{
    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Native;

    [TestClass]
    public class GeneralUnwrapperTest: UnitTestFor<IMemoryUnwrapper>
    {
        [TestMethod]
        public void CantUnwrapDspBitmaps()
        {
            Assert.IsFalse(
                systemUnderTest.CanUnwrap(ClipboardNativeApi.CF_DSPBITMAP));
        }

        [TestMethod]
        public void CantUnwrapDspEnhancedMetafile()
        {
            Assert.IsFalse(
                systemUnderTest.CanUnwrap(ClipboardNativeApi.CF_DSPENHMETAFILE));
        }

        [TestMethod]
        public void CantUnwrapEnhancedMetafile()
        {
            Assert.IsFalse(
                systemUnderTest.CanUnwrap(ClipboardNativeApi.CF_ENHMETAFILE));
        }

        [TestMethod]
        public void CantUnwrapMetafilePicture()
        {
            Assert.IsFalse(
                systemUnderTest.CanUnwrap(ClipboardNativeApi.CF_METAFILEPICT));
        }
    }
}