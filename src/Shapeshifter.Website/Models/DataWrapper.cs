using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shapeshifter.Website.Models
{
	public class DataWrapper<T>
    {
		[JsonProperty("data")]
		public T Data { get; set; }
		
		[JsonProperty("included")]
		public JArray Included { get; set; }
	}
}
