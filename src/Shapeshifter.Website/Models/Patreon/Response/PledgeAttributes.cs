namespace Shapeshifter.Website.Models.Patreon.Response
{
	using System;

	using Newtonsoft.Json;

	public class PledgeAttributes
  {
    [JsonProperty("amount_cents")]
    public int AmountCents { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("pledge_cap_cents")]
    public int? PledgeCapCents { get; set; }

    [JsonProperty("patron_pays_fees")]
    public bool PatronPaysFees { get; set; }
  }
}