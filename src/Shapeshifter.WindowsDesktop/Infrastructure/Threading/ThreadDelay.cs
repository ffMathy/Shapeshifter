namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System.Threading;
    using System.Threading.Tasks;

    using Interfaces;

    class ThreadDelay: IThreadDelay
    {
        public async Task ExecuteAsync(int millisecondsDelay)
        {
            await Task.Delay(millisecondsDelay);
        }

        public void Execute(int millisecondsDelay)
        {
            Thread.Sleep(millisecondsDelay);
        }
    }
}