namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.
    Interfaces
{
    using Keyboard.Interfaces;

    using Messages.Interfaces;

    public interface IPasteHotkeyInterceptor: 
        IPasteDetector,
        IWindowMessageInterceptor
    {
        bool IsEnabled { get; set; }

        void SkipNext();
    }
}