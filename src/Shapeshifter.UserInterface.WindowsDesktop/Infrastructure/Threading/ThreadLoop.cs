using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System;
using System.Threading;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    class ThreadLoop : IThreadLoop
    {
        public bool IsRunning
        {
            get; private set;
        }

        public void Start(Action action, CancellationToken token)
        {
            IsRunning = true;

            while(!token.IsCancellationRequested && IsRunning)
            {
                action();
            }

            Stop();
        }

        public void Stop()
        {
            IsRunning = false;
        }
    }
}
