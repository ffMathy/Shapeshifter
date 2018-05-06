namespace Shapeshifter.Website.Models
{
  using Newtonsoft.Json;

	public class UserRelationships
  {
    [JsonProperty("campaign")]
    public Campaign Campaign { get; set; }
  }
}