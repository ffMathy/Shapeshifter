namespace Shapeshifter.Website.Models.Patreon.Response
{
	using Newtonsoft.Json;

	public class Relationship
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }
	}
}
