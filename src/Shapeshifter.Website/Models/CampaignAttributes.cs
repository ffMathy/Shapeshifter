namespace Shapeshifter.Website.Models
{
  using System;

  using Newtonsoft.Json;
  using Newtonsoft.Json.Serialization;

  public class CampaignAttributes
  {
    [JsonProperty("summary")]
    public string Summary { get; set; }

    [JsonProperty("creation_name")]
    public string CreationName { get; set; }

    [JsonProperty("pay_per_name")]
    public string PayPerName { get; set; }

    [JsonProperty("one_liner")]
    public string OneLiner { get; set; }

    [JsonProperty("main_video_embed")]
    public string MainVideoEmbed { get; set; }

    [JsonProperty("main_video_url")]
    public string MainVideoUrl { get; set; }

    [JsonProperty("image_small_url")]
    public string ImageSmallUrl { get; set; }

    [JsonProperty("image_url")]
    public string ImageUrl { get; set; }

    [JsonProperty("thanks_video_url")]
    public string ThanksVideoUrl { get; set; }

    [JsonProperty("thanks_embed")]
    public string ThanksEmbed { get; set; }

    [JsonProperty("thanks_msg")]
    public string ThanksMessage { get; set; }

    [JsonProperty("pledge_url")]
    public string PledgeUrl { get; set; }

    [JsonProperty("is_monthly")]
    public bool IsMonthly { get; set; }

    [JsonProperty("is_nsfw")]
    public bool IsNotSuitableForWork { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonProperty("pledge_sum")]
    public int PledgeSum { get; set; }

    [JsonProperty("pledge_count")]
    public int PledgeCount { get; set; }

    [JsonProperty("patron_count")]
    public int PatronCount { get; set; }

    [JsonProperty("creation_count")]
    public int CreationCount { get; set; }

    [JsonProperty("outstanding_payment_amount_cents")]
    public int OutstandingPaymentAmountCents { get; set; }
  }
}