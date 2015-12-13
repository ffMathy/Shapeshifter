namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using Autofac;

    using Data.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Interfaces;
    using Messages.Interceptors.Interfaces;

    [TestClass]
    public class ClipboardInjectionServiceTest: TestBase
    {
        [TestMethod]
        public void InjectingDataSkipsNextCopyInterception()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IClipboardCopyInterceptor>();
                });

            var injectionService = container.Resolve<IClipboardInjectionService>();
            injectionService.InjectData(Substitute.For<IClipboardDataPackage>());

            var fakeInterceptor = container.Resolve<IClipboardCopyInterceptor>();
            fakeInterceptor.Received()
                           .SkipNext();
        }
    }
}