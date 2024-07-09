using System.Globalization;
using System.Text.Json;

namespace Debounce.Api.RabbitMq;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder MapEvent<T>(
        this IApplicationBuilder app,
        string queueName, Func<T, Task<bool>> messageHandler)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseRabbitMqEvents(queueName, message =>
        {
            try
            {
                T eventMessage;

                if (IsConvertable<T>())
                {
                    // Convert if it is a simple type
                    eventMessage = (T)Convert.ChangeType(message, typeof(T), CultureInfo.InvariantCulture);
                }
                else
                {
                    // otherwise expect a JSON string
                    eventMessage = JsonSerializer.Deserialize<T>(message)!;
                }

                return messageHandler.Invoke(eventMessage);
            }
            catch (Exception ex) when (ex is ArgumentNullException or JsonException or InvalidOperationException)
            {
                var logger = app.ApplicationServices.GetRequiredService<ILogger<RabbitMqService>>();
                logger.FailedHandlingMessage(message, ex);

                // Return false to indicate that the message was not handled
                return Task.FromResult(false);
            }
        });

        return app;
    }

    private static IApplicationBuilder UseRabbitMqEvents(this IApplicationBuilder app, string queueName, Func<string, Task<bool>> eventHandler)
    {
        var rabbitMqService = app.ApplicationServices.GetRequiredService<RabbitMqService>();
        rabbitMqService.ConsumeMessages(queueName, eventHandler);

        return app;
    }

    private static bool IsConvertable<T>()
    {
        var type = typeof(T);
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime);
    }
}
