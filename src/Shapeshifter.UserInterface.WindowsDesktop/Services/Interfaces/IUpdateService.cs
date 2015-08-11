using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IUpdateService
    {
        Task<bool> HasUpdatesAsync();

        Task UpdateAsync();
    }
}
