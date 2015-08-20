using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces
{
    public interface IDomainNameResolver
    {
        Task<bool> IsValidDomainAsync(string domain);

        Task<IEnumerable<IPAddress>> GetDomainIpAddressesAsync(string domain);
    }
}
