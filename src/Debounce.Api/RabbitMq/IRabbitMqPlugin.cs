namespace Debounce.Api.RabbitMq;

public interface IRabbitMqPlugin
{
    /// <summary>
    /// Occurs when the service is started and Exchanges and Queues are declared.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Occurs when the connection is established.
    /// Can occur multiple times if the connection is lost and re-established.
    /// </summary>
    Task OnConnectedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
