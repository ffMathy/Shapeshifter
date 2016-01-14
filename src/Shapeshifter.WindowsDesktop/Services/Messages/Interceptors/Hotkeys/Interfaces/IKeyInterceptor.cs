namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.
    Interfaces
{
    using System;
    using System.Windows.Input;

    public interface IKeyInterceptor: IHotkeyInterceptor
    {
        void AddInterceptingKey(IntPtr windowHandle, Key key);

        void RemoveInterceptingKey(IntPtr windowHandle, Key key);
    }
}