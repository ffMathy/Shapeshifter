namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IThreadLoop
    {
        bool IsRunning { get; }

        Task StartAsync(
            Func<Task> action,
            CancellationToken token = default(CancellationToken));

        void Stop();
    }
}