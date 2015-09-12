using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    public interface IThreadDelay
    {
        Task ExecuteAsync(int millisecondsDelay);
    }
}
