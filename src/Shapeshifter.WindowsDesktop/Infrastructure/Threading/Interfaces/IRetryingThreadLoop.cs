namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System.Threading.Tasks;

    public interface IRetryingThreadLoop
    {
        bool IsRunning { get; }

        Task StartAsync(RetryingThreadLoopJob job);

        void Stop();
    }
}