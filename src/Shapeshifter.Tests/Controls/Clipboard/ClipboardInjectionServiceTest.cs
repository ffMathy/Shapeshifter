using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Interfaces;

namespace Shapeshifter.Tests.Controls.Clipboard
{
    [TestClass]
    public class ClipboardInjectionServiceTest : TestBase
    {
        [TestMethod]
        public void InjectingDataSkipsNextCopyInterception()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IClipboardCopyInterceptor>();
            });

            var injectionService = container.Resolve<IClipboardInjectionService>();
            injectionService.InjectData(Substitute.For<IClipboardDataPackage>());

            var fakeInterceptor = container.Resolve<IClipboardCopyInterceptor>();
            fakeInterceptor.Received().SkipNext();
        }
    }
}