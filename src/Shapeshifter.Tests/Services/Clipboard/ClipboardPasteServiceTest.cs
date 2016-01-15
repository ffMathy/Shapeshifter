namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System;
    using System.Threading.Tasks;

    using Autofac;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Messages.Interceptors.Hotkeys.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Native.Interfaces;

    using NSubstitute;

    [TestClass]
    public class ClipboardPasteServiceTest: TestBase
    {
        [TestMethod]
        public async Task PasteDisablesAndEnablesPasteInterceptor()
        {
            var container = CreateContainer(
                c => {
                    c.RegisterFake<IThreadDelay>();
                    c.RegisterFake<IKeyboardNativeApi>();
                    c.RegisterFake<IPasteHotkeyInterceptor>();
                });

            var pasteService = container.Resolve<IClipboardPasteService>();
            await pasteService.PasteClipboardContentsAsync();

            var fakeInterceptor = container.Resolve<IPasteHotkeyInterceptor>();
            fakeInterceptor.Received()
                           .Uninstall();
            fakeInterceptor.Received()
                           .Install(Arg.Any<IntPtr>());
        }
    }
}