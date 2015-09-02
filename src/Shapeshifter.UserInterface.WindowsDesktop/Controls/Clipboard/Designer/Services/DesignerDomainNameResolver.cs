using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;
using System.Threading.Tasks;
using System.Net;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    class DesignerDomainNameResolver : IDomainNameResolver
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
