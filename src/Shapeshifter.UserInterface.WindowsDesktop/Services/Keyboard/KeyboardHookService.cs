using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using static Shapeshifter.UserInterface.WindowsDesktop.Services.Api.KeyboardApi;
using KeyEventHandler = Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces.KeyEventHandler;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    class KeyboardHookService : IKeyboardHookService, IDisposable
    {
        public event EventHandler<HookRecoveredEventArgument> HookRecovered;

        public event KeyEventHandler KeyUp;
        public event KeyEventHandler KeyDown;

        private IntPtr hookId;
        private KeyboardHookDelegate hookDelegate;

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
                using (var currentProcess = Process.GetCurrentProcess())
                using (var currentModule = currentProcess.MainModule)
                {
                    hookDelegate = LowLevelKeyboardCallback;
                    hookId = SetWindowsHookEx(
                        WH_KEYBOARD_LL,
                        hookDelegate,
                        GetModuleHandle(currentModule.ModuleName),
                        0);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private IntPtr LowLevelKeyboardCallback(int code, UIntPtr wParam, IntPtr lParam)
        {
            //catch the timing. if the call took too long, then there's a good chance we have disconnected the hook.
            var startTime = DateTime.UtcNow;

            var block = false;

            if (IsKeyEvent(code, ref wParam))
            {
                var keyEvent = (KeyEvent)wParam.ToUInt32();
                var virtualKeyCode = Marshal.ReadInt32(lParam);

                var key = KeyInterop.KeyFromVirtualKey(virtualKeyCode);
                HandleKeyEvent(ref block, keyEvent, virtualKeyCode);
            }

            var endTime = DateTime.UtcNow;
            var executionTime = endTime - startTime;

            if (HasHookProbablyDisconnected(executionTime))
            {
                //reconnect the hook.
                Reconnect();

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

            return CallNextHookEx(hookId, code, wParam, lParam);
        }

        private bool HasHookProbablyDisconnected(TimeSpan executionTime)
        {
            const int overhead = 25;
            return executionTime.TotalMilliseconds + overhead >= configuration.HookTimeout;
        }

        private void Reconnect()
        {
            Disconnect();
            Connect();
        }

        private void HandleKeyEvent(ref bool block, KeyEvent keyEvent, int virtualKeyCode)
        {
            switch (keyEvent)
            {
                case KeyEvent.WM_KEYDOWN:
                    if (KeyDown != null)
                    {
                        KeyDown(this, new KeyEventArgument(virtualKeyCode), ref block);
                    }
                    break;

                case KeyEvent.WM_KEYUP:
                    if (KeyDown != null)
                    {
                        KeyUp(this, new KeyEventArgument(virtualKeyCode), ref block);
                    }
                    break;
            }
        }

        private static bool IsKeyEvent(int code, ref UIntPtr wParam)
        {
            return code >= 0 && (
                            wParam.ToUInt32() == (int)KeyEvent.WM_KEYDOWN ||
                            wParam.ToUInt32() == (int)KeyEvent.WM_KEYUP ||
                            wParam.ToUInt32() == (int)KeyEvent.WM_SYSKEYDOWN ||
                            wParam.ToUInt32() == (int)KeyEvent.WM_SYSKEYUP);
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                UnhookWindowsHookEx(hookId);
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
