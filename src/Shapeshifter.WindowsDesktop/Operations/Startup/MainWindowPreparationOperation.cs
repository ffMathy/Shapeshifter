namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Controls.Window.Interfaces;

    using Interfaces;

    using Mediators.Interfaces;

    using Services.Messages.Interceptors.Hotkeys.Interfaces;
    using Services.Messages.Interfaces;

    public class MainWindowPreparationOperation: IMainWindowPreparationOperation
    {
        readonly IClipboardListWindow mainWindow;
        readonly IClipboardUserInterfaceInteractionMediator clipboardUserInterfaceInteractionMediator;
        readonly IMainWindowHandleContainer handleContainer;
        readonly IKeyInterceptor keyInterceptor;
        readonly IWindowMessageHook windowMessageHook;

        public MainWindowPreparationOperation(
            IClipboardListWindow mainWindow,
            IClipboardUserInterfaceInteractionMediator clipboardUserInterfaceInteractionMediator,
            IMainWindowHandleContainer handleContainer,
            IKeyInterceptor keyInterceptor,
            IWindowMessageHook windowMessageHook)
        {
            this.mainWindow = mainWindow;
            this.clipboardUserInterfaceInteractionMediator = clipboardUserInterfaceInteractionMediator;
            this.handleContainer = handleContainer;
            this.keyInterceptor = keyInterceptor;
            this.windowMessageHook = windowMessageHook;
        }

        public async Task RunAsync()
        {
            mainWindow.SourceInitialized +=
                OnMainWindowOnSourceInitialized;
            mainWindow.Show();
        }

        void OnMainWindowOnSourceInitialized(object sender, EventArgs e)
        {
            SetupKeyInterception();
            windowMessageHook.Connect(mainWindow);
            clipboardUserInterfaceInteractionMediator.Connect(mainWindow);
        }

        void SetupKeyInterception()
        {
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle,
                Key.Up);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle,
                Key.Down);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle,
                Key.Left);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle,
                Key.Right);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle,
                Key.Home);
            keyInterceptor.AddInterceptingKey(
                handleContainer.Handle,
                Key.Delete);
        }
    }
}