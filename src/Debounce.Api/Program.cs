using System.Text;

using Debounce.Api;
using Debounce.Api.RabbitMq;

using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddRabbitMqEventProvider();
builder.AddRabbitMqShovels();

var app = builder.Build();

app.MapEvent<string>("dedup_queue", message =>
{
    Console.WriteLine(" [x] Received {0}", message);
    return Task.FromResult(true);
});

var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5673
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

await DeclareExchanges(channel);

app.MapPost("/queue-job", (string message, int delay) =>
{
    var messageHash = message.ComputeSha256Hash();
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

await app.RunAsync();

async Task DeclareExchanges(IModel channel)
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
            { "x-cache-ttl", 5000 } // The Cache TimeToLife has to be as long as the longest delay,
                                    // but a common approach seems to be using multiple exchanges
                                    // with different TTLs
        });

    // Declare the intermediate queue that forwards to the deduplication exchange
    channel.QueueDeclare(queue: "delay_queue", durable: true, exclusive: false, autoDelete: false);
    channel.QueueBind(queue: "delay_queue", exchange: "delay_exchange", routingKey: "dedup_key");

    // Declare the deduplication queue
    channel.QueueDeclare(
        queue: "dedup_queue",
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: new Dictionary<string, object>
    {
        { "x-queue-type", "quorum" },
        { "x-message-deduplication", "true" }
    });

    // Bind the deduplication queue to the deduplication exchange
    channel.QueueBind(queue: "dedup_queue", exchange: "dedup_exchange", routingKey: "dedup_key");
}
