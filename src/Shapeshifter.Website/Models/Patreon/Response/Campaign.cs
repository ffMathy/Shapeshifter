﻿namespace Shapeshifter.Website.Models.Patreon.Response
{
	using Newtonsoft.Json;

	public class Campaign
  {
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("attributes")]
    public CampaignAttributes Attributes { get; set; }

    [JsonProperty("relationships")]
    public CampaignRelationships Relationships { get; set; }
  }
}