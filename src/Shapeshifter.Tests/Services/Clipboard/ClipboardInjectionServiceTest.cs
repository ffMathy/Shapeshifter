namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System.Threading.Tasks;

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
        public async Task InjectingDataSkipsNextCopyInterception()
        {
            await SystemUnderTest.InjectDataAsync(Substitute.For<IClipboardDataPackage>());

            var fakeInterceptor = Container.Resolve<IClipboardCopyInterceptor>();
            fakeInterceptor.Received()
                           .SkipNext();
        }
    }
}