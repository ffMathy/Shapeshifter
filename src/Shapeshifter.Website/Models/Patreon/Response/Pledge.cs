namespace Shapeshifter.Website.Models
{
	using Newtonsoft.Json;
	using Newtonsoft.Json.Serialization;

	public class Pledge
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("attributes")]
		public PledgeAttributes Attributes { get; set; }

		[JsonProperty("relationships")]
		public PledgeRelationships Relationships { get; set; }
	}
}