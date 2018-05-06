namespace Shapeshifter.Website.Models.Patreon.Response
{
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	public class DataWrapper<T>
    {
		[JsonProperty("data")]
		public T Data { get; set; }
		
		[JsonProperty("included")]
		public JArray Included { get; set; }
	}
}
