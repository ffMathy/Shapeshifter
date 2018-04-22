namespace Shapeshifter.WindowsDesktop.Services.Web.Updates.Interfaces
{
    using System;
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    public interface IUpdateService: ISingleInstance
    {
        Task UpdateAsync();
    }
}