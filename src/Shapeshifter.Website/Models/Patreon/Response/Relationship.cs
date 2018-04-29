using Newtonsoft.Json;

namespace Shapeshifter.Website.Models
{
	public class Relationship
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }
	}
}
