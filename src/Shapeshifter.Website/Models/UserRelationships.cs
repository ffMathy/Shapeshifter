namespace Shapeshifter.Website.Models
{
  using Newtonsoft.Json;
  using Newtonsoft.Json.Serialization;

  public class UserRelationships
  {
    [JsonProperty("campaign")]
    public Campaign Campaign { get; set; }
  }
}