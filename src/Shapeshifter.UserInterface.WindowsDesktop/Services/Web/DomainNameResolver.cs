using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Web
{
    [ExcludeFromCodeCoverage]
    class DomainNameResolver : IDomainNameResolver
    {
        public async Task<IEnumerable<IPAddress>> GetDomainIpAddressesAsync(string domain)
        {
            try {
                return await Dns.GetHostAddressesAsync(domain);
            } catch(SocketException)
            {
                return new IPAddress[0];
            }
        }

        public async Task<bool> IsValidDomainAsync(string domain)
        {
            var ipAddresses = await GetDomainIpAddressesAsync(domain);
            return ipAddresses.Any();
        }
    }
}
