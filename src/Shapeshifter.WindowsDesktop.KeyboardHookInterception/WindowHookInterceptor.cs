using EasyHook;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using static Shapeshifter.WindowsDesktop.Native.KeyboardNativeApi;

namespace Shapeshifter.WindowsDesktop.KeyboardHookInterception
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;

    public class WindowHookInterceptor : IEntryPoint
    {
        readonly HookHostCommunicator _interface;

        SetWindowsHookExDelegate originalSetWindowsHookExW;
        UnhookWindowsHookExDelegate originalUnhookWindowsHookEx;

        bool ctrlIsDown;

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        public WindowHookInterceptor(
               RemoteHooking.IContext InContext,
               string InChannelName)
        {
            _interface = RemoteHooking.IpcConnectClient<HookHostCommunicator>(InChannelName);
        }

        public IntPtr SetWindowsHookExOverride(int idHook, KeyboardHookDelegate lpfn, IntPtr hMod, uint dwThreadId)
        {
            var result = originalSetWindowsHookExW(
                idHook,
                (nCode, wParam, lParam) =>
                {
                    var vkCode = Marshal.ReadInt32(lParam);
                    var currentKey = KeyInterop.KeyFromVirtualKey(vkCode);

                    var keyEvent = (KeyEvent)wParam.ToUInt32();

                    var isCurrentKeyDown = keyEvent == KeyEvent.WM_KEYDOWN;

                    var shouldOverride = ctrlIsDown && (currentKey == Key.V);

                    var fetchOriginalResult = new Lazy<IntPtr>(() => lpfn(nCode, wParam, lParam));
                    if (!shouldOverride)
                    {
                        if (fetchOriginalResult.Value.ToInt64() != 4294967295) return fetchOriginalResult.Value;
                    }

                    switch (currentKey)
                    {
                        case Key.LeftCtrl:
                        case Key.RightCtrl:
                            ctrlIsDown = isCurrentKeyDown;
                            shouldOverride = true;
                            break;

                        case Key.V:
                            shouldOverride = ctrlIsDown;
                            break;
                    }

                    if (shouldOverride)
                    {
                        return CallNextHookEx(
                            new IntPtr(idHook), nCode, wParam, lParam);
                    }

                    return fetchOriginalResult.Value;
                },
                hMod,
                dwThreadId);
            return result;
        }

        public bool UnhookWindowsHookExOverride(IntPtr hhk)
        {
            var result = originalUnhookWindowsHookEx(hhk);
            return result;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate IntPtr SetWindowsHookExDelegate(int idHook, KeyboardHookDelegate lpfn, IntPtr hMod, uint dwThreadId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate bool UnhookWindowsHookExDelegate(IntPtr hhk);

        public void Run(
            RemoteHooking.IContext InContext,
            string InChannelName)
        {
            try
            {
                originalSetWindowsHookExW = InterceptUser32Method(
                    $"{nameof(SetWindowsHookEx)}W",
                    new SetWindowsHookExDelegate(SetWindowsHookExOverride));
                InterceptUser32Method(
                    $"{nameof(SetWindowsHookEx)}A",
                    new SetWindowsHookExDelegate(SetWindowsHookExOverride));
                originalUnhookWindowsHookEx = InterceptUser32Method(
                    $"{nameof(UnhookWindowsHookEx)}",
                    new UnhookWindowsHookExDelegate(UnhookWindowsHookExOverride));
            }
            catch (Exception exception)
            {
                _interface.ReportException(exception);
                return;
            }

            try
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    _interface.Ping();
                }
            }
            catch
            {
                // NET Remoting will raise an exception if host is unreachable
            }
        }

        TOriginalDelegate InterceptUser32Method<TOriginalDelegate>(string methodName, TOriginalDelegate callback)
        {
            _interface.DebugWriteLine("Intercepting " + methodName + ".");

            const string library = "user32.dll";

            var originalDelegate = LocalHook.GetProcDelegate<TOriginalDelegate>(
                library,
                methodName);

            var hook = LocalHook.Create(
                LocalHook.GetProcAddress(library, methodName),
                (Delegate)(object)callback,
                null);
            hook.ThreadACL.SetExclusiveACL(new int[] { });

            return originalDelegate;
        }
    }
}
