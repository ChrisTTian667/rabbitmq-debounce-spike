using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

using Microsoft.Extensions.Options;

namespace Debounce.Api.RabbitMq;

public class RabbitMqShovelService(
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IOptions<List<RabbitMqShovelOptions>> shovelConfigs,
    ILogger<RabbitMqShovelService> logger) : IRabbitMqPlugin
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var shovelOptions = shovelConfigs.Value;

        foreach (var shovelOption in shovelOptions)
        {
            await ApplyShovelConfigurationAsync(shovelOption, cancellationToken);
        }
    }

    private async Task ApplyShovelConfigurationAsync(
        [NotNull] RabbitMqShovelOptions shovelOptions,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var credentials = $"{rabbitMqOptions.Value.UserName}:{rabbitMqOptions.Value.Password}";
        var byteArray = Encoding.UTF8.GetBytes(credentials);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(byteArray));

        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            value = shovelOptions
        });

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var url =
                $"http://{rabbitMqOptions.Value.Host}:{rabbitMqOptions.Value.ManagementPort}/api/parameters/shovel/%2F/{shovelOptions.Name}";

            var response = await httpClient.PutAsync(
                new Uri(url),
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.ApplyShovelFailed(shovelOptions.Name, responseBody);
                throw new InvalidOperationException(
                    $"Failed to add shovel configuration: {response.ReasonPhrase}\n{responseBody}");
            }

            logger.ApplyShovel(shovelOptions.Name);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.ApplyShovelFailed(shovelOptions.Name, ex);
            throw;
        }
    }
}
