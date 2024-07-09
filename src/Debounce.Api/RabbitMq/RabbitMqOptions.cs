using System.Web;

using RabbitMQ.Client;

namespace Debounce.Api.RabbitMq;

public class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";

    public int Port { get; set; } = 5672;

    public int ManagementPort { get; set; } = 15672;

    public string VHost { get; set; } = string.Empty;

    public string? UserName { get; set; } = "guest";

    public string? Password { get; set; } = "guest";

    public bool Tls { get; set; }

    public int NetworkRecoveryInterval { get; set; } = 30;

    public string? EncodedPassword => HttpUtility.UrlEncode(Password);

    public IEnumerable<RabbitMqExchangeOptions> Exchanges { get; init; } = [];

    public IEnumerable<RabbitMqQueueOptions> Queues { get; init; } = [];

    public int MaxReconnectRetryCount { get; set; } = 5;
}

public class RabbitMqExchangeOptions
{
    public required string Name { get; init; }
    public string Type { get; init; } = ExchangeType.Direct;

    public bool Durable { get; set; }
    public bool AutoDelete { get; set; }

    public Dictionary<string, object> Arguments { get; init; } = new();
}

public class RabbitMqQueueOptions
{
    public required string Name { get; init; }

    public required string Exchange { get; init; }

    public required string RoutingKey { get; init; }

    public bool Durable { get; init; } = true;

    public bool Exclusive { get; init; }

    public bool AutoDelete { get; init; }

    public Dictionary<string, object> Arguments { get; init; } = new();
}
