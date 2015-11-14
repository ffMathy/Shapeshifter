using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Unwrappers.Interfaces;

namespace Shapeshifter.Tests.Controls.Clipboard.Unwrappers
{
    [TestClass]
    public class GeneralUnwrapperTest : TestBase
    {
        [TestMethod]
        public void CantUnwrapDspBitmaps()
        {
            var container = CreateContainer();

            var unwrapper = container.Resolve<IMemoryUnwrapper>();
            Assert.IsFalse(unwrapper.CanUnwrap(ClipboardApi.CF_DSPBITMAP));
        }

        [TestMethod]
        public void CantUnwrapDspEnhancedMetafile()
        {
            var container = CreateContainer();

            var unwrapper = container.Resolve<IMemoryUnwrapper>();
            Assert.IsFalse(unwrapper.CanUnwrap(ClipboardApi.CF_DSPENHMETAFILE));
        }

        [TestMethod]
        public void CantUnwrapEnhancedMetafile()
        {
            var container = CreateContainer();

            var unwrapper = container.Resolve<IMemoryUnwrapper>();
            Assert.IsFalse(unwrapper.CanUnwrap(ClipboardApi.CF_ENHMETAFILE));
        }

        [TestMethod]
        public void CantUnwrapMetafilePicture()
        {
            var container = CreateContainer();

            var unwrapper = container.Resolve<IMemoryUnwrapper>();
            Assert.IsFalse(unwrapper.CanUnwrap(ClipboardApi.CF_METAFILEPICT));
        }
    }
}