using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System.Threading;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    class ThreadDelay : IThreadDelay
    {
        public void Execute(int millisecondsDelay)
        {
            Thread.Sleep(millisecondsDelay);
        }
    }
}
