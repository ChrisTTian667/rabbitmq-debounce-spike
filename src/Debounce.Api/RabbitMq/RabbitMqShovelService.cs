using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

using Microsoft.Extensions.Options;

namespace Debounce.Api.RabbitMq;

public interface IRabbitMqPlugin
{
    Task OnStart()
    {
        return Task.CompletedTask;
    }

    Task OnConnected()
    {
        return Task.CompletedTask;
    }
}

public class RabbitMqShovelService(
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IOptions<IEnumerable<RabbitMqShovelOptions>> shovelConfigs,
    ILogger<RabbitMqShovelService> logger) : IRabbitMqPlugin
{
    public async Task OnStart()
    {
        var shovelOptions = shovelConfigs.Value;

        foreach (var shovelOption in shovelOptions)
        {
            await ApplyShovelConfigurationAsync(shovelOption);
        }
    }

    private async Task ApplyShovelConfigurationAsync([NotNull] RabbitMqShovelOptions shovelOptions)
    {
        using var httpClient = new HttpClient();
        var credentials = $"{rabbitMqOptions.Value.UserName}:{rabbitMqOptions.Value.Password}";
        var byteArray = Encoding.UTF8.GetBytes(credentials);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(byteArray));

        var json = System.Text.Json.JsonSerializer.Serialize(shovelOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PutAsync(
                new Uri($"{rabbitMqOptions.Value.Host}:_rabbitMQSettings./api/parameters/shovel/%2F/{shovelOptions.Name}"),
                content);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                logger.ApplyShovelFailed(shovelOptions.Name, responseBody);
                throw new InvalidOperationException(
                    $"Failed to add shovel configuration: {response.ReasonPhrase}\n{responseBody}");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.ApplyShovelFailed(shovelOptions.Name, ex.Message);
            throw;
        }
    }
}
