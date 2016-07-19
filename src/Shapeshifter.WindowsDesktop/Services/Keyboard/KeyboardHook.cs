using Shapeshifter.WindowsDesktop.Services.Keyboard.Interfaces;
using System;

using Shapeshifter.WindowsDesktop.Infrastructure.Events;
using System.Windows.Input;
using static Shapeshifter.WindowsDesktop.Native.KeyboardNativeApi;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System.Diagnostics.CodeAnalysis;

    class KeyboardHook : IKeyboardHook
    {
        delegate void KeyboardCallback(KeyEvent keyEvent, int vkCode, ref bool block);

        KeyboardCallback _hookedKeyboardCallback;
        KeyboardHookDelegate _hookedKeyboardHook;

        bool _ctrlIsDown;

        IntPtr _hookId;

        public bool IsConnected
        {
            get;
            private set;
        }

        public event EventHandler<KeyDetectedArgument> KeyDetected;

        public void Connect()
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("The keyboard hook is already connected.");
            }

            _hookedKeyboardHook = LowLevelKeyboardProc;
            _hookId = SetHook(_hookedKeyboardHook);
            _hookedKeyboardCallback = KeyboardListener_KeyboardCallbackAsync;

            IsConnected = true;
        }

        static IntPtr SetHook(KeyboardHookDelegate hook)
        {
            using (var currentProcess = Process.GetCurrentProcess())
            using (var currentModule = currentProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, hook,
                                        GetModuleHandle(currentModule.ModuleName), 0);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam)
        {
            var block = false;

            if (nCode >= 0)
            {
                if ((wParam.ToUInt32() == (int)KeyEvent.WM_KEYDOWN) ||
                    (wParam.ToUInt32() == (int)KeyEvent.WM_KEYUP) ||
                    (wParam.ToUInt32() == (int)KeyEvent.WM_SYSKEYDOWN) ||
                    (wParam.ToUInt32() == (int)KeyEvent.WM_SYSKEYUP))
                {
                    _hookedKeyboardCallback((KeyEvent)wParam.ToUInt32(), Marshal.ReadInt32(lParam), ref block);
                }
            }

            if (block)
            {
                return new IntPtr(-1);
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// HookCallbackAsync procedure that calls accordingly the KeyDown or KeyUp events.
        /// </summary>
        /// <param name="keyEvent">The type of press performed.</param>
        /// <param name="vkCode">The keycode pressed.</param>
        /// <param name="block">Wether or not the key should be blocked.</param>
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        void KeyboardListener_KeyboardCallbackAsync(KeyEvent keyEvent, int vkCode, ref bool block)
        {
            var key = KeyInterop.KeyFromVirtualKey(vkCode);
            var keyState = keyEvent == KeyEvent.WM_KEYDOWN ?
                KeyStates.Down :
                KeyStates.None;

            if ((key == Key.LeftCtrl) || (key == Key.RightCtrl))
            {
                _ctrlIsDown = keyState == KeyStates.Down;
            }

            var eventObject = new KeyDetectedArgument(
                key,
                keyState,
                _ctrlIsDown);
            KeyDetected?.Invoke(this, eventObject);

            block = eventObject.Cancel;
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("The keyboard hook is already disconnected.");
            }

            UnhookWindowsHookEx(_hookId);

            _hookedKeyboardHook = null;
            _hookedKeyboardCallback = null;

            _hookId = default(IntPtr);
        }
    }
}
