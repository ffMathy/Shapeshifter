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

        bool ControlNeeded { get; }

        bool NoRepeat { get; }
    }
}