namespace Shapeshifter.Website.Models.Patreon.Response
{
	using Newtonsoft.Json;

	public class UserRelationships
  {
    [JsonProperty("campaign")]
    public Campaign Campaign { get; set; }
  }
}