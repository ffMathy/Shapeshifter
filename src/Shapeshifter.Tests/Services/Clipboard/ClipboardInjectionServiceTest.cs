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
            SystemUnderTest.InjectData(Substitute.For<IClipboardDataPackage>());

            var fakeInterceptor = Container.Resolve<IClipboardCopyInterceptor>();
            fakeInterceptor.Received()
                           .SkipNext();
        }
    }
}