#region

using System;
using System.Windows.Threading;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    internal class UserInterfaceThread : IUserInterfaceThread
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