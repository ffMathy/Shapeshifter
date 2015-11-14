#region

using System.Threading.Tasks;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    public interface IThreadDelay
    {
        Task ExecuteAsync(int millisecondsDelay);
    }
}