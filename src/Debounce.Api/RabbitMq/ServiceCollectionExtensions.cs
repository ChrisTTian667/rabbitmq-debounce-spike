using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Debounce.Api.RabbitMq;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqEventProvider(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMQ"));
        services.Configure<List<RabbitMqShovelOptions>>(configuration.GetSection("Shovels"));
        services.TryAddSingleton<RabbitMqService>();

        return services;
    }

    public static IServiceCollection AddRabbitMqShovels(this IServiceCollection services)
    {
        services.TryAddSingleton<IRabbitMqPlugin, RabbitMqShovelService>();
        return services;
    }
}
