using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Debounce.Api.RabbitMq;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddRabbitMqEventProvider(this WebApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Services.AddRabbitMqEventProvider(app.Configuration);
        
        app.Services.Configure<RabbitMqOptions>(app.Configuration.GetSection("RabbitMQ"));
        app.Services.Configure<List<RabbitMqShovelOptions>>(app.Configuration.GetSection("Shovels"));
        app.Services.TryAddSingleton<RabbitMqService>();
        
        return app;
    }

    public static WebApplicationBuilder AddRabbitMqShovels(this WebApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Services.TryAddSingleton<IRabbitMqPlugin, RabbitMqShovelService>();
        return app;
    }    
}