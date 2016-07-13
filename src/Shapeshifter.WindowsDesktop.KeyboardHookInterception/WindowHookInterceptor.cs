using EasyHook;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Shapeshifter.WindowsDesktop.KeyboardHookInterception
{
    public class WindowHookInterceptor: IEntryPoint
    {
        public HookHostCommunicator Interface { get; private set; }

        public WindowHookInterceptor(
               RemoteHooking.IContext InContext,
               string InChannelName)
        {
            Interface = RemoteHooking.IpcConnectClient<HookHostCommunicator>(InChannelName);
        }
        
        static public bool MessageBeepHook(uint uType)
        {
            return false;
        }
        
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate bool MessageBeepDelegate(uint uType);

        public void Run(
            RemoteHooking.IContext InContext,
            string InChannelName)
        {
            try
            {
                var hook = LocalHook.Create(
                         LocalHook.GetProcAddress("user32.dll", "MessageBeep"),
                         new MessageBeepDelegate(MessageBeepHook),
                         null);
                hook.ThreadACL.SetExclusiveACL(new int[] { });
            }
            catch (Exception exception)
            {
                Interface.ReportException(exception);
                return;
            }
            
            try
            {
                while (true)
                {
                    Thread.Sleep(500);
                    Interface.Ping();
                }
            }
            catch
            {
                // NET Remoting will raise an exception if host is unreachable
            }
        }
    }
}
