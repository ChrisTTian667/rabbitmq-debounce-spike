using System.Globalization;
using System.Text;
using Debounce.Api;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Register the hosted service
builder.Services.AddHostedService<RabbitMqConsumerService>();

var app = builder.Build();

app.MapPost("/queue-job", (string message, int delay) =>
{
    var factory = new ConnectionFactory() { HostName = "localhost", Port = 5673 };
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

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
        exchange: "dedup_exchange",
        routingKey: "dedup_key",
        basicProperties: properties,
        body: body);

    return Results.Ok($"Job with id {properties.MessageId} queued successfully");
});

Declare();
app.Run();

void Declare()
{
    var factory = new ConnectionFactory() { HostName = "localhost", Port = 5673 };
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    channel.ExchangeDeclare(
        exchange: "dedup_exchange",
        type: "x-message-deduplication",
        arguments: new Dictionary<string, object>
        {
            { "x-delayed-type", "direct" },
            { "x-message-deduplication", "true" },
            { "x-cache-size", 10000 },
            { "x-cache-ttl", 5000 }                 // The TimeToLife has to be as long as the longest delay
        });

    channel.QueueDeclare(queue: "dedup_queue", durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
    {
        { "x-queue-type", "quorum" },
        { "x-message-deduplication", "true" }
    });

    channel.QueueBind(queue: "dedup_queue", exchange: "dedup_exchange", routingKey: "dedup_key");
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
