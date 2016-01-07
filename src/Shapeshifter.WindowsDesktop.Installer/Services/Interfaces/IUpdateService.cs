namespace Shapeshifter.UserInterface.WindowsDesktop.Installer.Services.Interfaces
{
    using System.Threading.Tasks;

    using Shapeshifter.WindowsDesktop.Shared.Infrastructure.Dependencies.Interfaces;

    public interface IUpdateService: ISingleInstance
    {
        Task UpdateAsync();
    }
}