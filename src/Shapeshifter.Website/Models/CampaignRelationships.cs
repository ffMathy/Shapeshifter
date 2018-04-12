namespace Shapeshifter.Website.Models
{
  using Newtonsoft.Json;
  using Newtonsoft.Json.Serialization;

  public class CampaignRelationships
  {
    [JsonProperty("creator")]
    public User Creator { get; set; }

    [JsonProperty("rewards")]
    public Reward[] Rewards { get; set; }

    [JsonProperty("goals")]
    public Goal[] Goals { get; set; }

    [JsonProperty("pledges")]
    public Pledge[] Pledges { get; set; }
  }
}