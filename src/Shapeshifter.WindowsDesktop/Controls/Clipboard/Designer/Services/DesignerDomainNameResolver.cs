namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using System.Net;
    using System.Threading.Tasks;

    using WindowsDesktop.Services.Web.Interfaces;

    using Interfaces;

    class DesignerDomainNameResolver
        : IDomainNameResolver,
          IDesignerService
    {
        public async Task<IPAddress[]> GetDomainIpAddressesAsync(string domain)
        {
            return null;
        }

        public async Task<bool> IsValidDomainAsync(string domain)
        {
            return true;
        }
    }
}