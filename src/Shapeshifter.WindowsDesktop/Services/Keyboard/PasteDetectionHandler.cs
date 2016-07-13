namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System;

    using Interfaces;

    using Messages.Interceptors.Hotkeys.Interfaces;

    using Stability.Interfaces;

    class PasteDetectionHandler: IPasteDetectionHandler
    {
        readonly IKeyboardDominanceWatcher keyboardDominanceWatcher;
        readonly IKeyboardHook keyboardHook;
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;

        public bool IsConnected { get; set; }

        public event EventHandler PasteDetected;

        public PasteDetectionHandler(
            IKeyboardDominanceWatcher keyboardDominanceWatcher,
            IKeyboardHook keyboardHook,
            IPasteHotkeyInterceptor pasteHotkeyInterceptor)
        {
            this.keyboardDominanceWatcher = keyboardDominanceWatcher;
            this.keyboardHook = keyboardHook;
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;

            SetupDominanceWatcher();
            SetupPasteHotkeyInterceptor();
        }

        void SetupPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.PasteDetected += PasteHotkeyInterceptor_PasteDetected;
        }

        void PasteHotkeyInterceptor_PasteDetected(object sender, EventArgs e)
        {
            OnPasteDetected();
        }
        void SetupDominanceWatcher()
        {
            keyboardDominanceWatcher.KeyboardAccessOverruled += KeyboardDominanceWatcher_KeyboardAccessOverruled;
            keyboardDominanceWatcher.KeyboardAccessRestored += KeyboardDominanceWatcher_KeyboardAccessRestored;
        }

        void KeyboardDominanceWatcher_KeyboardAccessRestored(object sender, EventArgs e)
        {
            keyboardHook.Disconnect();
        }

        void KeyboardDominanceWatcher_KeyboardAccessOverruled(object sender, EventArgs e)
        {
            keyboardHook.Connect();
        }

        public void Disconnect()
        {
            keyboardDominanceWatcher.Stop();
            if (keyboardHook.IsConnected)
            {
                keyboardHook.Disconnect();
            }
            IsConnected = false;
        }

        public void Connect()
        {
            keyboardDominanceWatcher.Start();
            IsConnected = true;
        }

        protected virtual void OnPasteDetected()
        {
            PasteDetected?.Invoke(this, EventArgs.Empty);
        }
    }
}
