using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Caching.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Web
{
    [ExcludeFromCodeCoverage]
    class DomainNameResolver : IDomainNameResolver
    {
        readonly IKeyValueCache<string, IPAddress[]> domainResolveCache;

        public DomainNameResolver(
            IKeyValueCache<string, IPAddress[]> domainResolveCache)
        {
            this.domainResolveCache = domainResolveCache;
        }

        public async Task<IPAddress[]> GetDomainIpAddressesAsync(string domain)
        {
            if(domain == null)
            {
                throw new ArgumentNullException(nameof(domain));
            }

            try {
                return await domainResolveCache.ThunkifyAsync(NormalizeDomain(domain), Dns.GetHostAddressesAsync);
            } catch(SocketException)
            {
                return new IPAddress[0];
            }
        }

        string NormalizeDomain(string domain)
        {
            return domain
                .Trim()
                .ToLowerInvariant();
        }

        public async Task<bool> IsValidDomainAsync(string domain)
        {
            var ipAddresses = await GetDomainIpAddressesAsync(domain);
            return ipAddresses.Any();
        }
    }
}
