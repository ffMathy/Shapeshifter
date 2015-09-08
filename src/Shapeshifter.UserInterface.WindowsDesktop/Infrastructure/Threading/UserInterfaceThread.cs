using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System;
using System.Windows.Threading;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    class UserInterfaceThread : IUserInterfaceThread
    {
        private readonly Dispatcher targetThread;

        public UserInterfaceThread(Dispatcher targetThread)
        {
            this.targetThread = targetThread;
        }

        public void Invoke(Action action)
        {
            targetThread.Invoke(action);
        }
    }
}
