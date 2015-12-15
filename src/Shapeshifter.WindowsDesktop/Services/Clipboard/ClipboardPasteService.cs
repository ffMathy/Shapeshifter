namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Controls.Window.Interfaces;

    using Infrastructure.Logging.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Keyboard;
    using Keyboard.Interfaces;

    using Messages.Interceptors.Hotkeys.Interfaces;

    class ClipboardPasteService: IClipboardPasteService
    {
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;
        readonly ILogger logger;
        readonly IMainWindowHandleContainer handleContainer;
        readonly IKeyboardManager keyboardManager;

        public ClipboardPasteService(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            ILogger logger,
            IMainWindowHandleContainer handleContainer,
            IKeyboardManager keyboardManager)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.logger = logger;
            this.handleContainer = handleContainer;
            this.keyboardManager = keyboardManager;
        }

        public async Task PasteClipboardContentsAsync()
        {
            var isVDown = keyboardManager.IsKeyDown(Key.V);
            var isCtrlDown = keyboardManager.IsKeyDown(Key.LeftCtrl);

            SimulatePaste(isCtrlDown, isVDown);

            logger.Information("Paste simulated.", 1);
        }

        void SimulatePaste(bool isCtrlDown, bool isVDown)
        {
            logger.Information(
                $"Simulating paste with CTRL {(isCtrlDown ? "down" : "released")} and V {(isVDown ? "down" : "released")}.");

            DisablePasteHotkeyInterceptor();
            UninstallPasteHotkeyInterceptor();

            RunFirstKeyboardPhase(isCtrlDown, isVDown);

            InstallPasteHotkeyInterceptor();

            RunSecondKeyboardPhase(isCtrlDown, isVDown);

            EnablePasteHotkeyInterceptor();
        }

        void EnablePasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.IsEnabled = true;
            logger.Information("Enabled paste hotkey interceptor.");
        }

        void DisablePasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.IsEnabled = false;
            logger.Information("Disabled paste hotkey interceptor.");
        }

        void RunFirstKeyboardPhase(bool isCtrlDown, bool isVDown)
        {
            var firstPhaseOperations = GetFirstPhaseKeyOperations(isCtrlDown, isVDown);
            keyboardManager.SendKeys(firstPhaseOperations.ToArray());
        }

        void RunSecondKeyboardPhase(bool isCtrlDown, bool isVDown)
        {
            if (!isCtrlDown && !isVDown)
            {
                return;
            }

            var secondPhaseOperations = GetSecondPhaseKeyOperations(isCtrlDown, isVDown);
            keyboardManager.SendKeys(secondPhaseOperations.ToArray());
        }

        static List<KeyOperation> GetSecondPhaseKeyOperations(bool isCtrlDown, bool isVDown)
        {
            var secondPhaseOperations = new List<KeyOperation>();
            if (isCtrlDown)
            {
                secondPhaseOperations.Add(new KeyOperation(Key.LeftCtrl, KeyDirection.Down));
            }

            if (isVDown)
            {
                secondPhaseOperations.Add(new KeyOperation(Key.V, KeyDirection.Down));
            }
            return secondPhaseOperations;
        }

        static List<KeyOperation> GetFirstPhaseKeyOperations(bool isCtrlDown, bool isVDown)
        {
            var operations = new List<KeyOperation>();

            if (!isCtrlDown)
            {
                operations.Add(new KeyOperation(Key.LeftCtrl, KeyDirection.Down));
            }

            if (!isVDown)
            {
                operations.Add(new KeyOperation(Key.V, KeyDirection.Down));
            }

            operations.Add(new KeyOperation(Key.V, KeyDirection.Up));
            operations.Add(new KeyOperation(Key.LeftCtrl, KeyDirection.Up));

            return operations;
        }

        void InstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.Install(handleContainer.Handle);
            logger.Information("Installed paste hotkey interceptor.");
        }

        void UninstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.Uninstall();
            logger.Information("Uninstalled paste hotkey interceptor.");
        }
    }
}