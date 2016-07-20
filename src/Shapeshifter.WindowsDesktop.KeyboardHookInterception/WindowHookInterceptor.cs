using EasyHook;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using static Shapeshifter.WindowsDesktop.Native.KeyboardNativeApi;

namespace Shapeshifter.WindowsDesktop.KeyboardHookInterception
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Input;

    public class WindowHookInterceptor : IEntryPoint
    {
        readonly HookHostCommunicator _interface;

        SetWindowsHookExDelegate originalSetWindowsHookExW;
        UnhookWindowsHookExDelegate originalUnhookWindowsHookEx;

        bool ctrlIsDown;
        bool vIsDown;

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        public WindowHookInterceptor(
               RemoteHooking.IContext InContext,
               string InChannelName)
        {
            _interface = RemoteHooking.IpcConnectClient<HookHostCommunicator>(
                InChannelName);
            _interface.Ping();
        }

        public IntPtr SetWindowsHookExOverride(int idHook, KeyboardHookDelegate lpfn, IntPtr hMod, uint dwThreadId)
        {
            _interface.DebugWriteLine("Hook install intercepted and overriden.");

            var result = originalSetWindowsHookExW(
                idHook,
                (nCode, wParam, lParam) =>
                {
                    var vkCode = Marshal.ReadInt32(lParam);
                    var currentKey = KeyInterop.KeyFromVirtualKey(vkCode);

                    var keyEvent = (KeyEvent)wParam.ToUInt32();

                    var isCurrentKeyDown = keyEvent == KeyEvent.WM_KEYDOWN;
                    var shouldIgnoreHook = _interface.GetShouldIgnoreHook();
                    var shouldOverride = ctrlIsDown && 
                        (currentKey == Key.V) && 
                        !shouldIgnoreHook;

                    switch (currentKey)
                    {
                        case Key.LeftCtrl:
                        case Key.RightCtrl:
                            ctrlIsDown = isCurrentKeyDown;
                            break;

                        case Key.V:
                            vIsDown = isCurrentKeyDown;
                            break;
                    }

                    var fetchOriginalResult = new Lazy<IntPtr>(() => lpfn(nCode, wParam, lParam));
                    if (!shouldOverride)
                    {
                        var value = fetchOriginalResult.Value.ToInt32();
                        _interface.DebugWriteLine($"Original call for {currentKey} {(isCurrentKeyDown ? "down" : "up")} returned {value}.");

                        if ((value != 1) && (value != -1))
                        {
                            return fetchOriginalResult.Value;
                        }
                    }

                    switch (currentKey)
                    {
                        case Key.LeftCtrl:
                        case Key.RightCtrl:
                            shouldOverride = true;
                            break;

                        case Key.V:
                            shouldOverride = ctrlIsDown;
                            break;

                        case Key.Down:
                        case Key.Up:
                        case Key.Left:
                        case Key.Right:
                            shouldOverride = ctrlIsDown && vIsDown;
                            break;
                    }

                    if (shouldOverride || shouldIgnoreHook)
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
            _interface.DebugWriteLine("Running keyboard hook interceptor.");
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
                _interface.DebugWriteLine("Intercepted successfully.");
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
                _interface.DebugWriteLine("Keyboard hook interceptor has been disconnected.");
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
