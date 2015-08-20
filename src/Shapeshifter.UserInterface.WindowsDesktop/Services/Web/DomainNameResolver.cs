using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Web
{
    [ExcludeFromCodeCoverage]
    class DomainNameResolver : IDomainNameResolver
    {
        public async Task<IEnumerable<IPAddress>> GetDomainIpAddressesAsync(string domain)
        {
            return await Dns.GetHostAddressesAsync(domain);
        }

        public async Task<bool> IsValidDomainAsync(string domain)
        {
            var ipAddresses = await GetDomainIpAddressesAsync(domain);
            return ipAddresses.Any();
        }
    }
}
