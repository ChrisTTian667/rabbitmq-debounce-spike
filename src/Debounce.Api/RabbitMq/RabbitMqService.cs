using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Debounce.Api.RabbitMq;

public sealed class RabbitMqService : IDisposable
{
    private readonly IOptions<RabbitMqOptions> _options;
    private readonly ILogger<RabbitMqService> _logger;
    private IConnection _connection = null!;
    private IModel _channel = null!;
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _reconnectSemaphore = new(1, 1);

    public RabbitMqService(IOptions<RabbitMqOptions> options, ILogger<RabbitMqService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        var optionValues = options.Value;

        _logger = logger;

        _factory = new ConnectionFactory()
        {
            HostName = optionValues.Host,
            Port = optionValues.Port
        };

        Connect(true);

        optionValues.Exchanges.ToList().ForEach(DeclareExchange);
        optionValues.Queues.ToList().ForEach(DeclareAndBindQueue);
    }

    private void Connect(bool tryReconnect = false)
    {
        try
        {
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

            _connection.ConnectionShutdown += OnConnectionShutdown;
            _connection.CallbackException += OnCallbackException;
            _connection.ConnectionBlocked += OnConnectionBlocked;
        }
        catch (Exception)
        {
            _logger.FailedToConnect();

            if (tryReconnect)
                Reconnect();
            else
                throw;
        }
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", e.ReplyText);
        Reconnect();
    }

    private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "RabbitMQ callback exception");
        Reconnect();
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection blocked: {Reason}", e.Reason);
        Reconnect();
    }

    private void Reconnect()
    {
        _reconnectSemaphore.Wait();

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
                    Connect();

                    if (_connection?.IsOpen != true)
                        continue;

                    _logger.SuccessfullyReconnected();

                    break;
                }
                catch (BrokerUnreachableException)
                {
                    _logger.FailedToReconnect(retryCount + 1);

                    // Exponential backoff
                    Thread.Sleep(TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryCount), 60)));
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

    public void ConsumeMessages(string queueName, Func<string, Task<bool>> messageHandler)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (_, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Task.Run(async () => await messageHandler(message))
                .ContinueWith(task =>
                {
                    if (task.IsFaulted || !task.Result)
                    {
                        if (task.IsFaulted)
                            _logger.FailedHandlingMessage(message, task.Exception);

                        _channel.BasicNack(eventArgs.DeliveryTag, false, true);
                    }
                    else
                        _channel.BasicAck(eventArgs.DeliveryTag, false);

                }, TaskScheduler.Default);
        };

        _channel.BasicConsume(
            queueName,
            false,
            consumer);
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
        if (!disposing)
            return;

        _channel.Dispose();
        _connection.Dispose();
        _reconnectSemaphore.Dispose();
    }
}
