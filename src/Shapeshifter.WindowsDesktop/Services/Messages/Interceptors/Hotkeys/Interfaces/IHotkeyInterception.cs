namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.
    Interfaces
{
    using System;
    using System.Windows.Input;

    public interface IHotkeyInterception
    {
        void Start(IntPtr windowHandle);

        void Stop(IntPtr windowHandle);

        int InterceptionId { get; }

        Key Key { get; }

        bool ControlKeyNeeded { get; }
        bool WinKeyNeeded { get; }
        bool ShiftKeyNeeded { get; }
        bool AltKeyNeeded { get; }

        bool NoRepeat { get; }
    }
}