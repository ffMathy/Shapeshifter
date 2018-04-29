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
        readonly IMainWindow mainWindow;
        readonly IClipboardUserInterfaceInteractionMediator clipboardUserInterfaceInteractionMediator;
        readonly IMainWindowHandleContainer handleContainer;
        readonly IKeyInterceptor keyInterceptor;
        readonly IWindowMessageHook windowMessageHook;
		readonly ISourceClipboardQuantityOverlay sourceClipboardQuantityOverlay;

		public MainWindowPreparationOperation(
            IMainWindow mainWindow,
            IClipboardUserInterfaceInteractionMediator clipboardUserInterfaceInteractionMediator,
            IMainWindowHandleContainer handleContainer,
            IKeyInterceptor keyInterceptor,
            IWindowMessageHook windowMessageHook,
			ISourceClipboardQuantityOverlay sourceClipboardQuantityOverlay)
        {
            this.mainWindow = mainWindow;
            this.clipboardUserInterfaceInteractionMediator = clipboardUserInterfaceInteractionMediator;
            this.handleContainer = handleContainer;
            this.keyInterceptor = keyInterceptor;
            this.windowMessageHook = windowMessageHook;
			this.sourceClipboardQuantityOverlay = sourceClipboardQuantityOverlay;

			SetupWindowMessageHook();
        }

        void SetupWindowMessageHook()
        {
            windowMessageHook.TargetWindow = mainWindow;
        }

        public async Task RunAsync()
        {
			await RunWindowAsync(mainWindow);
			await RunWindowAsync(sourceClipboardQuantityOverlay);
		}

		async Task RunWindowAsync(IWindow window)
		{
			window.SourceInitialized +=
				OnMainWindowOnSourceInitialized;
			window.Show();
		}

        void OnMainWindowOnSourceInitialized(object sender, EventArgs e)
        {
            windowMessageHook.Connect();
            clipboardUserInterfaceInteractionMediator.Connect();
			SetupKeyInterception();

			mainWindow.Hide();
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