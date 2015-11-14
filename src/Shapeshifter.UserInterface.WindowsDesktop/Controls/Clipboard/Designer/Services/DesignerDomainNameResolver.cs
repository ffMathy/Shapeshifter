using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    [ExcludeFromCodeCoverage]
    internal class DesignerDomainNameResolver : IDomainNameResolver
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