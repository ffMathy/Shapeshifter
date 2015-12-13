namespace Shapeshifter.WindowsDesktop.Services.Web.Interfaces
{
    using System.Net;
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    public interface IDomainNameResolver: ISingleInstance
    {
        Task<bool> IsValidDomainAsync(string domain);

        Task<IPAddress[]> GetDomainIpAddressesAsync(string domain);
    }
}