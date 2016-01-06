namespace Shapeshifter.WindowsDesktop.Shared.Services.Interfaces
{
    using System.Threading.Tasks;

    using WindowsDesktop.Infrastructure.Dependencies.Interfaces;

    public interface IUpdateService: ISingleInstance
    {
        Task UpdateAsync();
    }
}