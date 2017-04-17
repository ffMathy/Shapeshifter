using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Shapeshifter.Website.Controllers
{
    using System.Net;
    using System.Net.Http;

    using Microsoft.ApplicationInsights.AspNetCore.Extensions;

    using Newtonsoft.Json;

    public class PatreonController : Controller
    {
        readonly ConfigurationReader _secretReader;

        public PatreonController()
        {
            _secretReader = new ConfigurationReader("secrets.json");
        }

        public async Task<IActionResult> Postback(
            string code,
            string state)
        {
            var clientId = _secretReader.Read("patreon:clientId");
            var clientSecret = _secretReader.Read("patreon:clientSecret");

            var redirectUri = HttpContext
                .Request
                .GetUri();
            var redirectUriString = redirectUri
                .ToString()
                .Replace(redirectUri.Query, "");

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync("https://api.patreon.com/oauth2/token?code=" + WebUtility.UrlEncode(code) + "&grant_type=authorization_code&client_id=" + clientId + "&client_secret=" + clientSecret + "&redirect_uri=" + WebUtility.UrlEncode(redirectUriString), null);
                var json = await response.Content.ReadAsStringAsync();

                var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
                var accessToken = jsonObject.access_token;

                return Redirect;
            }
        }
    }
}