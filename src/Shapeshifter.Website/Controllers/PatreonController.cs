using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Shapeshifter.Website.Controllers
{
    using System.Net;
    using System.Net.Http;

    public class PatreonController : Controller
    {
        public async Task<IActionResult> Postback(
            string code, 
            string state)
        {
            var clientId = "TODO";
            var clientSecret = "TODO";
            var redirectUri = "TODO";
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync("https://api.patreon.com/oauth2/token?code=" + WebUtility.UrlEncode(code) + "&grant_type=authorization_code&client_id=" + clientId + "&client_secret=" + clientSecret + "&redirect_uri=" + WebUtility.UrlEncode(redirectUri), null);
                var json = await response.Content.ReadAsStringAsync();
            }
            return null;
        }
    }
}