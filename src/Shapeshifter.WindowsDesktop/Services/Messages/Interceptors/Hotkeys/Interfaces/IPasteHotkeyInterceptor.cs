namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.
    Interfaces
{
    public interface IPasteHotkeyInterceptor: IHotkeyInterceptor {

        bool IsEnabled { get; set; }

        void SkipNext();

    }
}