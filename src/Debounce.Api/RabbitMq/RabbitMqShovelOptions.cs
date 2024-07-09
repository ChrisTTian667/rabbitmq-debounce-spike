using System.Text.Json.Serialization;
using System.Web;

namespace Debounce.Api.RabbitMq;

public class RabbitMqShovelOptions
{
    private string _name = "";

    [JsonIgnore]
    public string Name
    {
        get => HttpUtility.UrlEncode(_name.Trim());
        set => _name = value;
    }

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
