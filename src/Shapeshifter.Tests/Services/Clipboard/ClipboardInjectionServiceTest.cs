namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using Autofac;

    using Data.Interfaces;

    using Interfaces;

    using Messages.Interceptors.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class ClipboardInjectionServiceTest: UnitTestFor<IClipboardInjectionService>
    {
        [TestMethod]
        public void InjectingDataSkipsNextCopyInterception()
        {
            systemUnderTest.InjectData(Substitute.For<IClipboardDataPackage>());

            var fakeInterceptor = container.Resolve<IClipboardCopyInterceptor>();
            fakeInterceptor.Received()
                           .SkipNext();
        }
    }
}