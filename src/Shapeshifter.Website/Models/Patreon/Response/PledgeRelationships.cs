namespace Shapeshifter.Website.Models
{
  using Newtonsoft.Json;

	public class PledgeRelationships
  {
    [JsonProperty("patron")]
    public User Patron { get; set; }
    
    [JsonProperty("reward")]
    public Reward Reward { get; set; }
    
    [JsonProperty("creator")]
    public User Creator { get; set; }

    [JsonProperty("address")]
    public Address Address { get; set; }

    [JsonProperty("card")]
    public Card Card { get; set; }

    [JsonProperty("pledge_vat_location")]
    public VatLocation VatLocation { get; set; }
  }
}