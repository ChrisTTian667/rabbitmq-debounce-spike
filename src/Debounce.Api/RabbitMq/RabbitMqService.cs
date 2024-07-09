using System.Text;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Debounce.Api.RabbitMq;

public sealed class RabbitMqService : IHostedService, IDisposable
{
    private bool _disposed;
    private readonly IOptions<RabbitMqOptions> _options;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly List<IRabbitMqPlugin> _plugins;
    private IConnection _connection = null!;
    private IModel _channel = null!;
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _reconnectSemaphore = new(1, 1);
    private readonly List<QueueHandler> _messageHandler = [];

    public RabbitMqService(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqService> logger,
        IEnumerable<IRabbitMqPlugin> plugins)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        var optionValues = options.Value;

        _logger = logger;
        _plugins = plugins.ToList();

        _factory = new ConnectionFactory()
        {
            HostName = optionValues.Host,
            Port = optionValues.Port,
            DispatchConsumersAsync = true
        };
    }

    private async Task ConnectAsync(bool tryReconnect = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

            _connection.ConnectionShutdown += OnConnectionShutdown;
            _connection.CallbackException += OnCallbackException;
            _connection.ConnectionBlocked += OnConnectionBlocked;

            await Task.WhenAll(_plugins.Select(plugin =>
                plugin.OnConnectedAsync(cancellationToken)));

            foreach (var handler in _messageHandler)
                CreateConsumer(handler.QueueName, handler.MessageDelegate);
        }
        catch (Exception)
        {
            _logger.FailedToConnect();

            if (tryReconnect)
                await ReconnectAsync();
            else
                throw;
        }
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.ConnectionShutdown(e.ReplyText);
        _ = Task.Run(ReconnectAsync).ConfigureAwait(false);
    }

    private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        _logger.CallbackException(e.Exception);
        _ = Task.Run(ReconnectAsync).ConfigureAwait(false);
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        _logger.ConnectionBlocked(e.Reason);
        _ = Task.Run(ReconnectAsync).ConfigureAwait(false);
    }

    private async Task ReconnectAsync()
    {
        await _reconnectSemaphore.WaitAsync();

        try
        {
            if (_connection is { IsOpen: true })
                return;

            _logger.Reconnecting();

            var retryCount = 0;
            var maxRetryCount = _options.Value.MaxReconnectRetryCount;

            while (_connection is null or { IsOpen: false } && (retryCount < maxRetryCount || maxRetryCount < 0))
            {
                try
                {
                    await ConnectAsync();

                    if (_connection?.IsOpen != true)
                        continue;

                    _logger.SuccessfullyReconnected();

                    break;
                }
                catch (BrokerUnreachableException)
                {
                    _logger.FailedToReconnect(retryCount + 1);

                    // Exponential backoff
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryCount), 60)));
                    retryCount++;
                }
            }

            if (_connection is null or { IsOpen: false })
            {
                _logger.GivingUpReconnect(maxRetryCount);
                throw new RabbitMqConnectionException("Failed to reconnect to RabbitMQ");
            }
        }
        finally
        {
            _reconnectSemaphore.Release();
        }
    }

    public void RegisterHandler(string queueName, Func<string, Task<bool>> messageHandler) =>
        _messageHandler.Add(new(queueName, messageHandler));

    private void CreateConsumer(string queueName, Func<string, Task<bool>> messageHandler)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var result = await messageHandler(message);

            if (result)
                _channel.BasicAck(eventArgs.DeliveryTag, false);
            else
                _channel.BasicNack(eventArgs.DeliveryTag, false, true);
        };

        _channel.BasicConsume(queueName, false, consumer);
    }

    private void DeclareExchange(RabbitMqExchangeOptions exchangeOptions)
    {
        _channel.ExchangeDeclare(
            exchange: exchangeOptions.Name,
            type: exchangeOptions.Type,
            autoDelete: exchangeOptions.AutoDelete,
            durable: exchangeOptions.Durable,
            arguments: exchangeOptions.Arguments);

        _logger.DeclaredExchange(exchangeOptions.Name);
    }

    private void DeclareAndBindQueue(RabbitMqQueueOptions queueOptions)
    {
        _channel.QueueDeclare(
            queueOptions.Name,
            queueOptions.Durable,
            queueOptions.Exclusive,
            queueOptions.AutoDelete,
            queueOptions.Arguments);

        _logger.DeclaredQueue(queueOptions.Name);

        _channel.QueueBind(
            queueOptions.Name,
            queueOptions.Exchange,
            queueOptions.RoutingKey);

        _logger.BoundQueue(queueOptions.Name, queueOptions.Exchange);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~RabbitMqService()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (!disposing) return;

        _channel.Dispose();
        _connection.Dispose();
        _reconnectSemaphore.Dispose();

        _disposed = true;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ConnectAsync(true, cancellationToken);

            _options.Value.Exchanges.ToList().ForEach(DeclareExchange);
            _options.Value.Queues.ToList().ForEach(DeclareAndBindQueue);

            await Task.WhenAll(_plugins.Select(plugin =>
                plugin.StartAsync(cancellationToken)));
        }
        catch (Exception ex)
        {
            _logger.StartError(ex);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    private record struct QueueHandler(string QueueName, Func<string, Task<bool>> MessageDelegate);
}
