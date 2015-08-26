using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces
{
    public interface IHotkeyInterception
    {
        void Start(IntPtr windowHandle);

        void Stop(IntPtr windowHandle);

        int InterceptionId { get; }

        int Hotkey { get; }

        bool ControlNeeded { get; }

        bool NoRepeat { get; }
    }
}
