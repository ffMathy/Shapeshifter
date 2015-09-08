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

            //var thread = new Thread(() => Run(action, token));
            //thread.SetApartmentState(ApartmentState.STA);
            //thread.IsBackground = true;
            //thread.Start();
            Run(action, token);
        }

        private async void Run(Action action, CancellationToken token)
        {
            while (!token.IsCancellationRequested && IsRunning)
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
