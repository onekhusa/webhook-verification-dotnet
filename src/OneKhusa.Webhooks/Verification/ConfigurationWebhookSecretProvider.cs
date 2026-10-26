using Microsoft.Extensions.Options;
using OneKhusa.Webhooks.Configuration;

namespace OneKhusa.Webhooks.Verification;

/// <summary>Reads per-event secrets from <c>OneKhusa:Webhooks:Secrets</c>. Picks up configuration reloads.</summary>
public sealed class ConfigurationWebhookSecretProvider(IOptionsMonitor<OneKhusaWebhookOptions> options) : IWebhookSecretProvider
{
    public string? GetSecret(string eventName)
    {
        foreach (var (configuredEvent, secret) in options.CurrentValue.Secrets)
        {
            if (string.Equals(configuredEvent, eventName, StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(secret) ? null : secret;
        }

        return null;
    }
}
