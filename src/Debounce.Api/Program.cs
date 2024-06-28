using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;

using Debounce.Api;

using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Register the hosted service
builder.Services.AddHostedService<RabbitMqConsumerService>();

var app = builder.Build();

var factory = new ConnectionFactory() { HostName = "localhost", Port = 5673 };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// Declare the exchanges, queues, and shovel configuration
await Declare(channel);

app.MapPost("/queue-job", (string message, int delay) =>
{
    var messageHash = ComputeSha256Hash(message);
    var properties = channel.CreateBasicProperties();
    properties.Headers = new Dictionary<string, object>
    {
        { "x-delay", delay },
        { "x-deduplication-header", messageHash }
    };

    properties.MessageId = messageHash;  // Use message hash as MessageId
    var body = Encoding.UTF8.GetBytes(message);

    channel.BasicPublish(
        exchange: "delay_exchange",
        routingKey: "dedup_key",
        basicProperties: properties,
        body: body);

    return Results.Ok($"Job with id {properties.MessageId} queued successfully");
});

app.Run();

async Task Declare(IModel channel)
{
    // Declare the first exchange for delay handling
    channel.ExchangeDeclare(
        exchange: "delay_exchange",
        type: "x-delayed-message",
        arguments: new Dictionary<string, object>
        {
            { "x-delayed-type", "direct" },
        });

    // Declare the second exchange for deduplication
    channel.ExchangeDeclare(
        exchange: "dedup_exchange",
        type: "x-message-deduplication",
        arguments: new Dictionary<string, object>
        {
            { "x-delayed-type", "direct" },
            { "x-message-deduplication", "true" },
            { "x-cache-size", 10000 },
            { "x-cache-ttl", 5000 } // The TimeToLife has to be as long as the longest delay
        });

    // Declare the intermediate queue that forwards to the deduplication exchange
    channel.QueueDeclare(queue: "delay_queue", durable: true, exclusive: false, autoDelete: false);
    channel.QueueBind(queue: "delay_queue", exchange: "delay_exchange", routingKey: "dedup_key");

    // Declare the deduplication queue
    channel.QueueDeclare(queue: "dedup_queue", durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
    {
        { "x-queue-type", "quorum" },
        { "x-message-deduplication", "true" }
    });

    // Bind the deduplication queue to the deduplication exchange
    channel.QueueBind(queue: "dedup_queue", exchange: "dedup_exchange", routingKey: "dedup_key");

    // Add shovel configuration
    await AddShovelConfiguration();
}

async Task AddShovelConfiguration()
{
    using var httpClient = new HttpClient();
    var byteArray = "guest:guest"u8.ToArray();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
        "Basic",
        Convert.ToBase64String(byteArray));

    var shovelConfig = new
    {
        value = new ShovelConfig
        {
            SrcUri = "amqp://localhost",
            SrcQueue = "delay_queue",
            DestUri = "amqp://localhost",
            DestExchange = "dedup_exchange",
            DestExchangeKey = "dedup_key",
            AckMode = "on-confirm",
            ReconnectDelay = 5
        }
    };

    using var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(shovelConfig), Encoding.UTF8, "application/json");

    var response = await httpClient.PutAsync(
        new Uri("http://localhost:15673/api/parameters/shovel/%2F/shovel_delay_to_dedup"), content);

    if (!response.IsSuccessStatusCode)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Failed to add shovel configuration: {response.ReasonPhrase}\n{responseBody}");

    }
}

static string ComputeSha256Hash(string rawData)
{
    var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
    var builder = new StringBuilder();
    foreach (var b in bytes)
    {
        builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
    }

    return builder.ToString();
}

public class ShovelConfig
{
    [JsonPropertyName("src-uri")]
    public string SrcUri { get; set; } = null!;

    [JsonPropertyName("src-queue")]
    public string SrcQueue { get; set; } = null!;

    [JsonPropertyName("dest-uri")]
    public string DestUri { get; set; } = null!;

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
