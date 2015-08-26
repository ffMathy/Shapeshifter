using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interfaces;
using System;
using System.Threading;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages
{
    class HotkeyInterception : IHotkeyInterception
    {
        const int InterceptionIdOffset = 13371337;

        static int interceptionId;

        public int InterceptionId { get; }

        public bool ControlNeeded
        {
            get; set;
        }

        public bool NoRepeat
        {
            get; set;
        }

        public int Hotkey
        {
            get; set;
        }

        public HotkeyInterception()
        {
            InterceptionId = Interlocked.Increment(ref interceptionId);
        }

        public void Start(IntPtr windowHandle)
        {
            Stop(windowHandle);

            var modifier = 0;
            if(ControlNeeded) modifier |= KeyboardApi.MOD_CONTROL;
            if(NoRepeat) modifier |= KeyboardApi.MOD_NOREPEAT;

            var registrationResult = KeyboardApi.RegisterHotKey(
                windowHandle, InterceptionId, modifier, Hotkey);
            if(!registrationResult)
            {
                throw new InvalidOperationException($"Couldn't install the hotkey interceptor for key {Hotkey}.");
            }
        }

        public void Stop(IntPtr windowHandle)
        {
            var registrationResult = KeyboardApi.UnregisterHotKey(
                windowHandle, InterceptionId);
            if(!registrationResult)
            {
                throw new InvalidOperationException($"Couldn't uninstall the hotkey interceptor for key {Hotkey}.");
            }
        }
    }
}
