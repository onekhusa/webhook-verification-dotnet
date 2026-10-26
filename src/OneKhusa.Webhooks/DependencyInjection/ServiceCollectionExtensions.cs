using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneKhusa.Webhooks.Configuration;
using OneKhusa.Webhooks.Handling;
using OneKhusa.Webhooks.Verification;

namespace OneKhusa.Webhooks.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers webhook verification and deduplication. You still need to register your own
    /// <see cref="IPushPaymentWebhookHandler"/> and map the endpoint with
    /// <see cref="Endpoints.WebhookEndpointExtensions.MapOneKhusaPushPaymentWebhook"/>.
    /// </summary>
    public static IServiceCollection AddOneKhusaWebhooks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OneKhusaWebhookOptions>().Bind(configuration.GetSection(OneKhusaWebhookOptions.SectionName));

        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IWebhookSecretProvider, ConfigurationWebhookSecretProvider>();
        services.TryAddSingleton<IWebhookSignatureVerifier, HmacSha512SignatureVerifier>();
        services.TryAddSingleton<IWebhookDeduplicator, InMemoryWebhookDeduplicator>();

        return services;
    }
}
