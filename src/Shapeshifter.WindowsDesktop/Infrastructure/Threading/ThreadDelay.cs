namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System.Threading;
    using System.Threading.Tasks;

    using Interfaces;

    class ThreadDelay: IThreadDelay
    {
        public void Execute(int millisecondsDelay)
        {
            Thread.Sleep(millisecondsDelay);
        }

        public async Task ExecuteAsync(int millisecondsDelay)
        {
            await Task.Delay(millisecondsDelay);
        }
    }
}