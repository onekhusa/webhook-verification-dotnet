using OneKhusa.Webhooks.Models;

namespace OneKhusa.Webhooks.Handling;

/// <summary>
/// Your business logic for verified push payment webhooks: mark orders paid, failed or reversed.
/// Only called after the signature has been verified. Register exactly one implementation.
/// </summary>
/// <remarks>
/// Keep it quick, and make it idempotent: the same event can be delivered more than once.
/// Throwing makes the endpoint return 500, which tells OneKhusa the delivery was not processed.
/// </remarks>
public interface IPushPaymentWebhookHandler
{
    Task HandleAsync(PushPaymentWebhook webhook, CancellationToken cancellationToken);
}

/// <param name="EventName">One of <see cref="PushPaymentEvents"/>.</param>
public sealed record PushPaymentWebhook(string EventName, PushPaymentWebhookPayload Payload, DateTimeOffset ReceivedAt);
