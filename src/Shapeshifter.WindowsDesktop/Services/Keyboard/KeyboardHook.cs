using Shapeshifter.WindowsDesktop.Services.Keyboard.Interfaces;
using System;
using System.Collections.Generic;
using Shapeshifter.WindowsDesktop.Infrastructure.Events;
using System.Windows.Input;
using static Shapeshifter.WindowsDesktop.Native.KeyboardNativeApi;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    class KeyboardHook : IKeyboardHook
    {
        private delegate void KeyboardCallback(KeyEvent keyEvent, int vkCode, ref bool block);

        private KeyboardCallback _hookedKeyboardCallback;
        private KeyboardHookDelegate _hookedKeyboardHook;

        private bool _ctrlIsDown;
        private IntPtr _hookId;

        public ICollection<Key> IgnoredKeys
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsConnected
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler<KeyDetectedArgument> KeyDetected;

        public void Connect()
        {
            _hookedKeyboardHook = LowLevelKeyboardProc;
            _hookId = SetHook(_hookedKeyboardHook);
            _hookedKeyboardCallback = KeyboardListener_KeyboardCallbackAsync;
        }

        private IntPtr SetHook(KeyboardHookDelegate hook)
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                using (var curModule = currentProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, hook,
                                            GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam)
        {
            bool block = false;

            if (nCode >= 0)
            {
                if (wParam.ToUInt32() == (int)KeyEvent.WM_KEYDOWN ||
                    wParam.ToUInt32() == (int)KeyEvent.WM_KEYUP ||
                    wParam.ToUInt32() == (int)KeyEvent.WM_SYSKEYDOWN ||
                    wParam.ToUInt32() == (int)KeyEvent.WM_SYSKEYUP)
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
        private void KeyboardListener_KeyboardCallbackAsync(KeyEvent keyEvent, int vkCode, ref bool block)
        {
            var key = KeyInterop.KeyFromVirtualKey(vkCode);
            var keyState = keyEvent == KeyEvent.WM_KEYDOWN ?
                KeyStates.Down :
                KeyStates.None;

            if(key == Key.LeftCtrl || key == Key.RightCtrl)
            {
                _ctrlIsDown = keyState == KeyStates.Down;
            }

            var eventObject = new KeyDetectedArgument(
                key,
                keyState,
                _ctrlIsDown);
            KeyDetected(this, eventObject);

            block = eventObject.Cancel;
        }

        public void Disconnect()
        {
            UnhookWindowsHookEx(_hookId);

            _hookedKeyboardHook = null;
            _hookedKeyboardCallback = null;

            _hookId = default(IntPtr);
        }
    }
}
