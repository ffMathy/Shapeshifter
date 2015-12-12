namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    using Interfaces;

    class ThreadDelay: IThreadDelay
    {
        
        public async Task ExecuteAsync(int millisecondsDelay)
        {
            await Task.Delay(millisecondsDelay);
        }
    }
}