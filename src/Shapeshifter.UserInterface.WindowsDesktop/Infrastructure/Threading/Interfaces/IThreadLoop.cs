namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IThreadLoop
    {
        bool IsRunning { get; }

        void Start(Func<Task> action, CancellationToken token);

        void Stop();
    }
}