using System.Text.Json.Serialization;

namespace Debounce.Api;

public class ShovelConfig
{
    [JsonPropertyName("src-uri")]
    public required Uri SrcUri { get; init; }

    [JsonPropertyName("src-queue")]
    public required string SrcQueue { get; init; }

    [JsonPropertyName("dest-uri")]
    public required Uri DestUri { get; init; }

    [JsonPropertyName("dest-exchange")]
    public required string DestExchange { get; init; }

    [JsonPropertyName("dest-exchange-key")]
    public required string DestExchangeKey { get; init; }

    [JsonPropertyName("ack-mode")]
    public string? AckMode { get; init; }

    [JsonPropertyName("reconnect-delay")]
    public int ReconnectDelay { get; init; } = 5;

    [JsonPropertyName("dest-add-forward-headers")]
    public bool AddForwardHeaders { get; init; } = true;
}
