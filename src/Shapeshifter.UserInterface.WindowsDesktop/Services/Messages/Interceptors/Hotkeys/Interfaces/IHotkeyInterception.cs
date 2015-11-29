namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.
    Interfaces
{
    using System;

    public interface IHotkeyInterception
    {
        void Start(IntPtr windowHandle);

        void Stop(IntPtr windowHandle);

        int InterceptionId { get; }

        int KeyCode { get; }

        bool ControlNeeded { get; }

        bool NoRepeat { get; }
    }
}