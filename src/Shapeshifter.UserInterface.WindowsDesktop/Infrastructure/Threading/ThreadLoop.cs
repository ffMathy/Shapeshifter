#region

using System;
using System.Threading;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    internal class ThreadLoop : IThreadLoop
    {
        public bool IsRunning { get; private set; }

        public void StartAsync(Func<Task> action, CancellationToken token)
        {
            IsRunning = true;
            RunAsync(action, token);
        }

        private async void RunAsync(Func<Task> action, CancellationToken token)
        {
            while (!token.IsCancellationRequested && IsRunning)
            {
                await action();
            }

            Stop();
        }

        public void Stop()
        {
            IsRunning = false;
        }
    }
}