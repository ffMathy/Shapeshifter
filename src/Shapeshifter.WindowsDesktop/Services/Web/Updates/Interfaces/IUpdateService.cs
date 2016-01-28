namespace Shapeshifter.WindowsDesktop.Services.Updates.Interfaces
{
    using System;
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    public interface IUpdateService: ISingleInstance
    {
        Task UpdateAsync();

        Version GetCurrentVersion();
    }
}