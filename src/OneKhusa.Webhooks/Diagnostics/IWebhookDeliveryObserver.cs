using OneKhusa.Webhooks.Verification;

namespace OneKhusa.Webhooks.Diagnostics;

/// <summary>
/// Optional hook that sees every delivery, including rejected ones. Intended for diagnostics such as
/// a webhook inspector page. Business logic belongs in <see cref="Handling.IPushPaymentWebhookHandler"/>.
/// </summary>
public interface IWebhookDeliveryObserver
{
    void OnDelivery(WebhookDelivery delivery);
}

/// <param name="Body">The raw body. It is unverified unless <see cref="Verification"/> is valid.</param>
/// <param name="Headers">The request headers as received.</param>
/// <param name="Outcome">What the endpoint did, for example "Processed", "Duplicate" or "Rejected".</param>
public sealed record WebhookDelivery(
    DateTimeOffset ReceivedAt,
    string? EventName,
    string? Signature,
    string Body,
    IReadOnlyDictionary<string, string> Headers,
    WebhookVerificationResult Verification,
    int StatusCode,
    string Outcome);
