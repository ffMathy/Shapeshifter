using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    class ThreadDelay : IThreadDelay
    {
        public async Task ExecuteAsync(int millisecondsDelay)
        {
            await Task.Delay(millisecondsDelay);
        }
    }
}
