namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.
    Interfaces
{
    using System;

    public interface IKeyInterceptor: IHotkeyInterceptor
    {
        void AddInterceptingKey(IntPtr windowHandle, int keyCode);

        void RemoveInterceptingKey(IntPtr windowHandle, int keyCode);
    }
}