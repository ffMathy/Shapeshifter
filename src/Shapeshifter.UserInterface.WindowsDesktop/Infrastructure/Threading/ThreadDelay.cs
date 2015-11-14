#region

using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    internal class ThreadDelay : IThreadDelay
    {
        public async Task ExecuteAsync(int millisecondsDelay)
        {
            await Task.Delay(millisecondsDelay);
        }
    }
}