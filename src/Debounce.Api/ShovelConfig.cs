using System.Text.Json.Serialization;

public class ShovelConfig
{
    [JsonPropertyName("src-uri")]
    public Uri SrcUri { get; set; } = null!;

    [JsonPropertyName("src-queue")]
    public string SrcQueue { get; set; } = null!;

    [JsonPropertyName("dest-uri")]
    public Uri DestUri { get; set; } = null!;

    [JsonPropertyName("dest-exchange")]
    public string DestExchange { get; set; } = null!;

    [JsonPropertyName("dest-exchange-key")]
    public string DestExchangeKey { get; set; } = null!;

    [JsonPropertyName("ack-mode")]
    public string AckMode { get; set; } = null!;

    [JsonPropertyName("reconnect-delay")]
    public int ReconnectDelay { get; set; } = 5;

    [JsonPropertyName("dest-add-forward-headers")]
    public bool AddForwardHeaders { get; set; } = true;
}
