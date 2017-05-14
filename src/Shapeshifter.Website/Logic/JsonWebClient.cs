using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shapeshifter.Website.Logic
{
  public class JsonWebClient : IJsonWebClient
  {
    public async Task<T> GetAsync<T>(string url)
    {
      using (var client = new HttpClient())
      {
        var blob = await client.GetStringAsync(url);
        return JsonConvert.DeserializeObject<T>(blob);
      }
    }
  }
}
