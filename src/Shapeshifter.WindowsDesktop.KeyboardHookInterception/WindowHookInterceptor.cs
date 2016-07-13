using EasyHook;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using static Shapeshifter.WindowsDesktop.Native.KeyboardNativeApi;

namespace Shapeshifter.WindowsDesktop.KeyboardHookInterception
{
    public class WindowHookInterceptor: IEntryPoint
    {
        private HookHostCommunicator _interface;

        public WindowHookInterceptor(
               RemoteHooking.IContext InContext,
               string InChannelName)
        {
            _interface = RemoteHooking.IpcConnectClient<HookHostCommunicator>(InChannelName);
        }
        
        public IntPtr SetWindowsHookEx(int idHook, KeyboardHookDelegate lpfn, IntPtr hMod, uint dwThreadId)
        {
            return new IntPtr(1337);
        }

        public bool UnhookWindowsHookEx(IntPtr hhk)
        {
            return true;
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
                InterceptUser32Method(
                    nameof(SetWindowsHookEx),
                    new SetWindowsHookExDelegate(SetWindowsHookEx));
                InterceptUser32Method(
                    nameof(UnhookWindowsHookEx),
                    new UnhookWindowsHookExDelegate(UnhookWindowsHookEx));
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

        private void InterceptUser32Method(string methodName, Delegate callback)
        {
            var hook = LocalHook.Create(
                LocalHook.GetProcAddress("user32.dll", methodName),
                callback,
                null);
            hook.ThreadACL.SetExclusiveACL(new int[] { });
        }
    }
}
