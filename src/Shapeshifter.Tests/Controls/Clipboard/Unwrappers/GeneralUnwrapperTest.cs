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
        public void CanUnwrapDspBitmaps()
        {
            var container = CreateContainer();

            var unwrapper = container.Resolve<IMemoryUnwrapper>();
            Assert.IsTrue(unwrapper.CanUnwrap(ClipboardApi.CF_DSPBITMAP));
        }

        [TestMethod]
        public void CanUnwrapDspEnhancedMetafile()
        {
            var container = CreateContainer();

            var unwrapper = container.Resolve<IMemoryUnwrapper>();
            Assert.IsTrue(unwrapper.CanUnwrap(ClipboardApi.CF_DSPENHMETAFILE));
        }

        [TestMethod]
        public void CanUnwrapEnhancedMetafile()
        {
            var container = CreateContainer();

            var unwrapper = container.Resolve<IMemoryUnwrapper>();
            Assert.IsTrue(unwrapper.CanUnwrap(ClipboardApi.CF_ENHMETAFILE));
        }

        [TestMethod]
        public void CanUnwrapMetafilePicture()
        {
            var container = CreateContainer();

            var unwrapper = container.Resolve<IMemoryUnwrapper>();
            Assert.IsTrue(unwrapper.CanUnwrap(ClipboardApi.CF_METAFILEPICT));
        }
    }
}