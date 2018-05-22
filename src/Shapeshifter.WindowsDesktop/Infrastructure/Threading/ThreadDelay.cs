namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System.Threading;
    using System.Threading.Tasks;

    using Interfaces;

    class ThreadDelay: IThreadDelay
    {
        public async Task ExecuteAsync(int millisecondsDelay)
		{
			if (millisecondsDelay < 1)
				return;

            await Task.Delay(millisecondsDelay);
        }

        public void Execute(int millisecondsDelay)
        {
			if (millisecondsDelay < 1)
				return;

            Thread.Sleep(millisecondsDelay);
        }
    }
}