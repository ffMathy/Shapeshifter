namespace Shapeshifter.WindowsDesktop.Operations.Startup
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Controls.Window.Interfaces;

    using Interfaces;

    using Mediators.Interfaces;
	using Serilog;

	using Services.Messages.Interceptors.Hotkeys.Interfaces;
    using Services.Messages.Interfaces;
	using Services.Window.Interfaces;

	public class MainWindowPreparationOperation: IMainWindowPreparationOperation
    {
        readonly IMainWindow mainWindow;
        readonly IClipboardUserInterfaceInteractionMediator clipboardUserInterfaceInteractionMediator;
        readonly IMainWindowHandleContainer handleContainer;
        readonly IKeyInterceptor keyInterceptor;
        readonly IWindowMessageHook windowMessageHook;
		readonly ISourceClipboardQuantityOverlay sourceClipboardQuantityOverlay;
		readonly ILogger logger;
		readonly IActiveWindowService activeWindowService;

		public MainWindowPreparationOperation(
            IMainWindow mainWindow,
            IClipboardUserInterfaceInteractionMediator clipboardUserInterfaceInteractionMediator,
            IMainWindowHandleContainer handleContainer,
            IKeyInterceptor keyInterceptor,
            IWindowMessageHook windowMessageHook,
			ISourceClipboardQuantityOverlay sourceClipboardQuantityOverlay,
			ILogger logger,
			IActiveWindowService activeWindowService)
        {
            this.mainWindow = mainWindow;
            this.clipboardUserInterfaceInteractionMediator = clipboardUserInterfaceInteractionMediator;
            this.handleContainer = handleContainer;
            this.keyInterceptor = keyInterceptor;
            this.windowMessageHook = windowMessageHook;
			this.sourceClipboardQuantityOverlay = sourceClipboardQuantityOverlay;
			this.logger = logger;
			this.activeWindowService = activeWindowService;
			SetupWindowMessageHook();
        }

        void SetupWindowMessageHook()
        {
            windowMessageHook.TargetWindow = mainWindow;
        }

        public async Task RunAsync()
		{
			logger.Verbose("Initializing clipboard quantity overlay and main window.");

			mainWindow.SourceInitialized += OnMainWindowOnSourceInitialized;
			mainWindow.Show();

			sourceClipboardQuantityOverlay.Show();
			sourceClipboardQuantityOverlay.Hide();
		}

        void OnMainWindowOnSourceInitialized(object sender, EventArgs e)
        {
			logger.Verbose("The main window source handle has been determined.");

			clipboardUserInterfaceInteractionMediator.Connect();
			SetupKeyInterception();

			activeWindowService.Connect();
            windowMessageHook.Connect();

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