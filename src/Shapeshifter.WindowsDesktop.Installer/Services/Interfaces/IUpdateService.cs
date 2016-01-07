namespace Shapeshifter.UserInterface.WindowsDesktop.Installer.Services.Interfaces
{
    using System.Threading.Tasks;

    using Shapeshifter.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

    public interface IUpdateService: ISingleInstance
    {
        Task UpdateAsync();
    }
}