namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Web
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    using Infrastructure.Caching.Interfaces;

    using Interfaces;

    [ExcludeFromCodeCoverage]
    class DomainNameResolver: IDomainNameResolver
    {
        readonly IKeyValueCache<string, IPAddress[]> domainResolveCache;

        public DomainNameResolver(
            IKeyValueCache<string, IPAddress[]> domainResolveCache)
        {
            this.domainResolveCache = domainResolveCache;
        }

        public async Task<IPAddress[]> GetDomainIpAddressesAsync(string domain)
        {
            if (domain == null)
            {
                throw new ArgumentNullException(nameof(domain));
            }

            try
            {
                return
                    await
                    domainResolveCache.ThunkifyAsync(
                                                     NormalizeDomain(domain),
                                                     Dns.GetHostAddressesAsync);
            }
            catch (SocketException)
            {
                return new IPAddress[0];
            }
        }

        static string NormalizeDomain(string domain)
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