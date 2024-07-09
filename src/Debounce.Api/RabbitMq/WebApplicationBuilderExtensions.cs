using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Debounce.Api.RabbitMq;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddRabbitMqEventProvider(this WebApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.Services.AddRabbitMqEventProvider(app.Configuration);
        return app;
    }

    public static WebApplicationBuilder AddRabbitMqShovels(this WebApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Services.TryAddSingleton<IRabbitMqPlugin, RabbitMqShovelService>();
        return app;
    }
}
