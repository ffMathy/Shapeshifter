namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System;
    using System.Windows.Input;

    using Interfaces;

    using Messages.Interceptors.Hotkeys.Interfaces;

    class PasteDetectionHandler: IPasteDetectionHandler
    {
        readonly IKeyboardDominanceWatcher keyboardDominanceWatcher;
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;

        public bool IsConnected { get; set; }

        public event EventHandler PasteDetected;

        public PasteDetectionHandler(
            IKeyboardDominanceWatcher keyboardDominanceWatcher,
            IPasteHotkeyInterceptor pasteHotkeyInterceptor)
        {
            this.keyboardDominanceWatcher = keyboardDominanceWatcher;
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            
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

        public void Disconnect()
        {
            keyboardDominanceWatcher.Stop();
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
