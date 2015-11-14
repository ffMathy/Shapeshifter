#region

using System;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces
{
    public interface IHotkeyInterceptor : IWindowMessageInterceptor
    {
        event EventHandler<HotkeyFiredArgument> HotkeyFired;
    }
}