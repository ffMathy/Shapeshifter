using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    interface IConsumerThreadLoop
    {
        bool IsRunning { get; }

        void Notify(Func<Task> action, CancellationToken token);

        void Stop();
    }
}
