namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.
    Interfaces
{
    using System;

    using Infrastructure.Events;

    using Messages.Interfaces;

    public interface IHotkeyInterceptor: IWindowMessageInterceptor
    {
        event EventHandler<HotkeyFiredArgument> HotkeyFired;
    }
}