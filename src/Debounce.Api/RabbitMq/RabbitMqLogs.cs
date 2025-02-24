namespace Debounce.Api.RabbitMq;

using Microsoft.Extensions.Logging;

public static partial class RabbitMqLogs
{
    [LoggerMessage(LogLevel.Error, "Error starting RabbitMQ service")]
    public static partial void StartError(this ILogger<RabbitMqService> logger, Exception error);

    [LoggerMessage(LogLevel.Information, "Successfully connected to RabbitMQ")]
    public static partial void SuccessfullyConnected(this ILogger<RabbitMqService> logger);

    [LoggerMessage(LogLevel.Information, "Successfully reconnected to RabbitMQ")]
    public static partial void SuccessfullyReconnected(this ILogger<RabbitMqService> logger);

    [LoggerMessage(LogLevel.Error, "Failed to connected to RabbitMQ")]
    public static partial void FailedToConnect(this ILogger<RabbitMqService> logger);

    [LoggerMessage(LogLevel.Error, "Reconnection attempt {Attempt} failed")]
    public static partial void FailedToReconnect(this ILogger<RabbitMqService> logger, int attempt);

    [LoggerMessage(LogLevel.Error, "Failed to reconnect after {MaxRetryCount} attempts. Giving up.")]
    public static partial void GivingUpReconnect(this ILogger<RabbitMqService> logger, int maxRetryCount);

    [LoggerMessage(LogLevel.Information, "Reconnecting to RabbitMQ...")]
    public static partial void Reconnecting(this ILogger<RabbitMqService> logger);

    [LoggerMessage(LogLevel.Information, "Declared exchange '{ExchangeName}'")]
    public static partial void DeclaredExchange(this ILogger<RabbitMqService> logger, string exchangeName);

    [LoggerMessage(LogLevel.Information, "Declared queue '{QueueName}'")]
    public static partial void DeclaredQueue(this ILogger<RabbitMqService> logger, string queueName);

    [LoggerMessage(LogLevel.Information, "Bound queue '{QueueName}' to Exchange '{ExchangeName}'")]
    public static partial void BoundQueue(this ILogger<RabbitMqService> logger, string queueName, string exchangeName);

    [LoggerMessage(LogLevel.Warning, "RabbitMQ connection blocked: {Reason}")]
    public static partial void ConnectionBlocked(this ILogger<RabbitMqService> logger, string reason);

    [LoggerMessage(LogLevel.Warning, "RabbitMQ connection shutdown: {Reason}")]
    public static partial void ConnectionShutdown(this ILogger<RabbitMqService> logger, string reason);

    [LoggerMessage(LogLevel.Error, "RabbitMQ callback exception")]
    public static partial void CallbackException(this ILogger<RabbitMqService> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to handle message '{Message}'")]
    public static partial void FailedHandlingMessage(this ILogger<RabbitMqService> logger, string message, Exception error);
}

public static partial class RabbitMqShovelLogs
{
    [LoggerMessage(LogLevel.Information, "Successfully applied Shovel configuration '{Shovel}'")]
    public static partial void ApplyShovel(this ILogger<RabbitMqShovelService> logger, string shovel);

    [LoggerMessage(LogLevel.Error, "Failed to add Shovel configuration '{Shovel}'")]
    public static partial void ApplyShovelFailed(this ILogger<RabbitMqShovelService> logger, string shovel, Exception error);

    [LoggerMessage(LogLevel.Error, "Failed to add Shovel configuration '{Shovel}', because '{Error}'")]
    public static partial void ApplyShovelFailed(this ILogger<RabbitMqShovelService> logger, string shovel, string error);
}
