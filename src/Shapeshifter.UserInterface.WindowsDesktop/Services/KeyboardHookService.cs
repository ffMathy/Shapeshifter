using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

using KeyEventHandler = Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces.KeyEventHandler;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class KeyboardHookService : IKeyboardHookService, IDisposable
    {
        public event EventHandler<HookRecoveredEventArgument> HookRecovered;

        public event KeyEventHandler KeyUp;
        public event KeyEventHandler KeyDown;

        private IntPtr hookId;

        private readonly IKeyboardHookConfiguration configuration;

        public KeyboardHookService(IKeyboardHookConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public bool IsConnected
        {
            get
            {
                return hookId != IntPtr.Zero;
            }
        }
        
        public void Connect()
        {
            if (!IsConnected)
            {
                hookId = SetHook(LowLevelKeyboardCallback);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private IntPtr LowLevelKeyboardCallback(int code, UIntPtr wParam, IntPtr lParam)
        {
            //catch the timing. if the call took too long, then there's a good chance we have disconnected the hook.
            var startTime = DateTime.UtcNow;

            var block = false;

            if (code >= 0 && (
                wParam.ToUInt32() == (int)KeyboardApi.KeyEvent.WM_KEYDOWN ||
                wParam.ToUInt32() == (int)KeyboardApi.KeyEvent.WM_KEYUP ||
                wParam.ToUInt32() == (int)KeyboardApi.KeyEvent.WM_SYSKEYDOWN ||
                wParam.ToUInt32() == (int)KeyboardApi.KeyEvent.WM_SYSKEYUP))
            {
                var keyEvent = (KeyboardApi.KeyEvent)wParam.ToUInt32();
                var virtualKeyCode = Marshal.ReadInt32(lParam);

                var key = KeyInterop.KeyFromVirtualKey(virtualKeyCode);
                switch (keyEvent)
                {
                    case KeyboardApi.KeyEvent.WM_KEYDOWN:
                        if (KeyDown != null)
                        {
                            KeyDown(this, new KeyEventArgument(virtualKeyCode), ref block);
                        }
                        break;

                    case KeyboardApi.KeyEvent.WM_KEYUP:
                        if (KeyDown != null)
                        {
                            KeyUp(this, new KeyEventArgument(virtualKeyCode), ref block);
                        }
                        break;
                }
            }

            var endTime = DateTime.UtcNow;
            var executionTime = endTime - startTime;

            const int Overhead = 25;
            if (executionTime.TotalMilliseconds + Overhead >= configuration.HookTimeout)
            {
                //reconnect the hook.
                Disconnect();
                Connect();

                //attempt to block the keystroke anyway.
                block = true;

                //signal the event.
                if (HookRecovered != null)
                {
                    HookRecovered(this, new HookRecoveredEventArgument());
                }
            }

            if (block)
            {
                return new IntPtr(-1);
            }

            return KeyboardApi.CallNextHookEx(hookId, code, wParam, lParam);
        }

        private IntPtr SetHook(KeyboardApi.KeyboardHookDelegate hook)
        {
            GC.KeepAlive(hook);

            using (var currentProcess = Process.GetCurrentProcess())
            using (var currentModule = currentProcess.MainModule)
            {
                return KeyboardApi.SetWindowsHookEx(
                    KeyboardApi.WH_KEYBOARD_LL,
                    hook,
                    KeyboardApi.GetModuleHandle(currentModule.ModuleName),
                    0);
            }
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                KeyboardApi.UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (IsConnected)
            {
                Disconnect();
            }
        }
    }
}
