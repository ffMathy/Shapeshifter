using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shapeshifter.Website
{
  using Models;
  using System.Net.Http;

  public class PatreonClient : IPatreonClient
  {
    const int campaignId = 557794;

    readonly string accessToken;

    public PatreonClient(
        string accessToken)
    {
      this.accessToken = accessToken;
    }

    public async Task<IEnumerable<Pledge>> GetPledges()
    {
    }
  }
}
