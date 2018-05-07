namespace Shapeshifter.Website.Models.Patreon.Response
{
	using System;

	using Newtonsoft.Json;

	public class UserAttributes
  {
    [JsonProperty("first_name")]
    public string FirstName { get; set; }

    [JsonProperty("last_name")]
    public string LastName { get; set; }

    [JsonProperty("full_name")]
    public string FullName { get; set; }

    [JsonProperty("gender")]
    public int Gender { get; set; }

    [JsonProperty("vanity")]
    public string Vanity { get; set; }

    [JsonProperty("about")]
    public string About { get; set; }

    [JsonProperty("facebook_id")]
    public string FacebookId { get; set; }

    [JsonProperty("image_url")]
    public string ImageUrl { get; set; }

    [JsonProperty("thumb_url")]
    public string ThumbUrl { get; set; }

    [JsonProperty("youtube")]
    public string YouTubeUrl { get; set; }

    [JsonProperty("twitter")]
    public string TwitterUrl { get; set; }

    [JsonProperty("facebook")]
    public string FacebookUrl { get; set; }

    [JsonProperty("is_suspended")]
    public bool IsSuspended { get; set; }

    [JsonProperty("is_deleted")]
    public bool IsDeleted { get; set; }

    [JsonProperty("is_nuked")]
    public bool IsNuked { get; set; }

    [JsonProperty("created")]
    public DateTime Created { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
  }
}