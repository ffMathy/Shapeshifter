using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shapeshifter.Website.Models
{
	using Newtonsoft.Json;

	public class User
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("attributes")]
		public UserAttributes Attributes { get; set; }

		[JsonProperty("relationships")]
		public UserRelationships Relationships { get; set; }

		[JsonProperty("data")]
		public Relationship Data { get; set; }
	}
}
