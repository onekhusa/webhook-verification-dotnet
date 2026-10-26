namespace OneKhusa.Webhooks.Verification;

/// <summary>Resolves the webhook secret for an event. Each OneKhusa webhook event has its own secret.</summary>
public interface IWebhookSecretProvider
{
    /// <returns>The secret, or <c>null</c> when none is configured for <paramref name="eventName"/>.</returns>
    string? GetSecret(string eventName);
}
