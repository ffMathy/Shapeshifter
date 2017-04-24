namespace Shapeshifter.Website.Models
{
  using System;

  using Newtonsoft.Json;
  using Newtonsoft.Json.Serialization;

  public class PledgeAttributes
  {
    [JsonProperty("amount_cents")]
    public int AmountCents { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("pledge_cap_cents")]
    public int PledgeCapCents { get; set; }

    [JsonProperty("patron_pays_fees")]
    public bool PatronPaysFees { get; set; }
  }
}