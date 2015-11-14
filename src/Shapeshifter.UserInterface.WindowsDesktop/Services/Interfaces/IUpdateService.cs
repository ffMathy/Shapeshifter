using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IUpdateService : ISingleInstance
    {
        Task UpdateAsync();
    }
}