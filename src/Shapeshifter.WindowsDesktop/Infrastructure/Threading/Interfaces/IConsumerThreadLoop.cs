namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IConsumerThreadLoop
    {
        bool IsRunning { get; }

        void Notify(
            Func<Task> action,
            CancellationToken token = default(CancellationToken));

        void Stop();
    }
}