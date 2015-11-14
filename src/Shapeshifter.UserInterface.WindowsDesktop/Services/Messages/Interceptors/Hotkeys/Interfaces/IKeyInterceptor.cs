#region

using System;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces
{
    public interface IKeyInterceptor : IHotkeyInterceptor
    {
        void AddInterceptingKey(IntPtr windowHandle, int keyCode);
        void RemoveInterceptingKey(IntPtr windowHandle, int keyCode);
    }
}