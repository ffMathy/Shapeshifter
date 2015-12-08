using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    using System.Security.Permissions;
    using System.Windows.Threading;

    using Autofac;

    using Interfaces;

    [TestClass]
    public class MainThreadInvokerTest : TestBase
    {
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame),
                frame);
            Dispatcher.PushFrame(frame);
        }

        static object ExitFrame(object frame)
        {
            ((DispatcherFrame)frame).Continue = false;
            return null;
        }

        [TestMethod]
        public void InvokesItemsOnMainThread()
        {
            var container = CreateContainer();

            var mainThreadInvoker = container.Resolve<IMainThreadInvoker>();

            var backgroundThreadId = 0;
            var dispatcherThreadId = 0;
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            var hasRun = false;
            var thread = new Thread(
                () =>
                {
                    backgroundThreadId = Thread.CurrentThread.ManagedThreadId;
                    mainThreadInvoker.Invoke(
                        () =>
                        {
                            dispatcherThreadId = Thread.CurrentThread.ManagedThreadId;
                        });
                    hasRun = true;
                });

            thread.Start();

            while (!hasRun)
            {
                DoEvents();
            }

            Assert.AreNotEqual(
                backgroundThreadId, dispatcherThreadId);
            Assert.AreNotEqual(
                backgroundThreadId, currentThreadId);
            Assert.AreEqual(
                dispatcherThreadId, currentThreadId);
            Assert.AreNotEqual(default(int), backgroundThreadId);
            Assert.AreNotEqual(default(int), dispatcherThreadId);
        }
    }
}
