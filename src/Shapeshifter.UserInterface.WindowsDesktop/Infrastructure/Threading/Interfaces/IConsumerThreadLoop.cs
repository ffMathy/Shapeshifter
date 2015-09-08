using System;
using System.Threading;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    interface IConsumerThreadLoop
    {
        bool IsRunning { get; }

        void Start(Action action, CancellationToken token);

        void Stop();

        void Notify();
    }
}
