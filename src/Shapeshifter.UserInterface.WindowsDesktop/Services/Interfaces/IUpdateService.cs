namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    public interface IUpdateService: ISingleInstance
    {
        Task UpdateAsync();
    }
}