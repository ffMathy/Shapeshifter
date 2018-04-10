using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
