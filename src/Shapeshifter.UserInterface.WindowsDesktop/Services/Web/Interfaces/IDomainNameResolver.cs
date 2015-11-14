using System.Net;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces
{
    public interface IDomainNameResolver : ISingleInstance
    {
        Task<bool> IsValidDomainAsync(string domain);

        Task<IPAddress[]> GetDomainIpAddressesAsync(string domain);
    }
}