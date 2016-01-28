namespace Shapeshifter.WindowsDesktop.Data.Unwrappers
{
    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Native;

    [TestClass]
    public class GeneralUnwrapperTest: UnitTestFor<IGeneralUnwrapper>
    {
        [TestMethod]
        public void CantUnwrapDspBitmaps()
        {
            Assert.IsFalse(
                SystemUnderTest.CanUnwrap(ClipboardNativeApi.CF_DSPBITMAP));
        }

        [TestMethod]
        public void CantUnwrapDspEnhancedMetafile()
        {
            Assert.IsFalse(
                SystemUnderTest.CanUnwrap(ClipboardNativeApi.CF_DSPENHMETAFILE));
        }

        [TestMethod]
        public void CantUnwrapEnhancedMetafile()
        {
            Assert.IsFalse(
                SystemUnderTest.CanUnwrap(ClipboardNativeApi.CF_ENHMETAFILE));
        }

        [TestMethod]
        public void CantUnwrapMetafilePicture()
        {
            Assert.IsFalse(
                SystemUnderTest.CanUnwrap(ClipboardNativeApi.CF_METAFILEPICT));
        }
    }
}