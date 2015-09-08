using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces
{
    public interface IKeyInterceptor : IHotkeyInterceptor
    {
        void AddInterceptingKey(IntPtr windowHandle, int keyCode);
        void RemoveInterceptingKey(IntPtr windowHandle, int keyCode);
    }
}
