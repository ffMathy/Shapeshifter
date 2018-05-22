namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Windows.Threading;

    using Interfaces;

    class UserInterfaceThread: IUserInterfaceThread
    {
        readonly Dispatcher targetThread;

        public UserInterfaceThread(Dispatcher targetThread)
        {
            this.targetThread = targetThread;
        }

        public void Invoke(Action action)
        {
            targetThread.Invoke(action);
        }

		public int Id => targetThread.Thread.ManagedThreadId;
	}
}