using OneKhusa.PushPayments.Web.Shared.Payments;
using OneKhusa.Webhooks;
using OneKhusa.Webhooks.Handling;
using OneKhusa.Webhooks.Models;

namespace OneKhusa.PushPayments.Web.Features.ReceivePushPaymentWebhook;

/// <summary>
/// The sample's business logic for verified push payment webhooks. This is the class you would
/// replace with your own: look up the order, then mark it paid, failed or reversed.
/// </summary>
public sealed class PushPaymentWebhookHandler(IPaymentStore store, ILogger<PushPaymentWebhookHandler> logger) : IPushPaymentWebhookHandler
{
    public Task HandleAsync(PushPaymentWebhook webhook, CancellationToken cancellationToken)
    {
        var payload = webhook.Payload;
        var reference = FindPaymentReference(payload);
        if (reference is null)
        {
            // Still acknowledged with 200: retrying will not make an unknown payment appear.
            logger.LogWarning(
                "Webhook {Event} does not match a known payment (sourceReferenceNumber {Source}, merchantReferenceNumber {Merchant})",
                webhook.EventName, payload.SourceReferenceNumber, payload.MerchantReferenceNumber);
            return Task.CompletedTask;
        }

        var updated = store.Update(reference, payment => Apply(payment, webhook));
        logger.LogInformation("Payment {Reference} is now {Status} after {Event}", reference, updated?.Status, webhook.EventName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// The webhook should echo the <c>sourceReferenceNumber</c> we sent. The docs' examples are
    /// ambiguous about which field carries it, so both are checked.
    /// </summary>
    private string? FindPaymentReference(PushPaymentWebhookPayload payload) =>
        new[] { payload.SourceReferenceNumber, payload.MerchantReferenceNumber }
            .FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate) && store.Find(candidate) is not null);

    /// <summary>Pure state transition: what a verified event means for a payment.</summary>
    public static PaymentRecord Apply(PaymentRecord payment, PushPaymentWebhook webhook)
    {
        var payload = webhook.Payload;
        var at = webhook.ReceivedAt;

        switch (webhook.EventName)
        {
            case PushPaymentEvents.Success:
                // A late redelivery of "success" must not undo a reversal.
                if (payment.Status == PaymentStatus.Reversed)
                    return payment.WithEvent(new PaymentEvent(at, "Ignored late success webhook", "Payment was already reversed."));

                return payment.WithEvent(new PaymentEvent(at, "Payment received",
                    $"Transaction {payload.TransactionReferenceNumber}, {payload.TransactionAmount:N2} from {payload.SourceAccountName ?? payload.SourceAccountNumber} via {payload.SourceInstitution}, fee {payload.TransactionFee:N2}."))
                    with
                    {
                        Status = PaymentStatus.Paid,
                        StatusDetail = null,
                        TransactionReferenceNumber = payload.TransactionReferenceNumber ?? payment.TransactionReferenceNumber,
                    };

            case PushPaymentEvents.Failed:
                var reason = payload.ErrorMessages is { Count: > 0 }
                    ? string.Join("; ", payload.ErrorMessages)
                    : $"Response code {payload.ResponseCode}";

                // A failure arriving after a success is suspicious; keep the money-moved status and flag it.
                if (payment.Status is PaymentStatus.Paid or PaymentStatus.Reversed)
                    return payment.WithEvent(new PaymentEvent(at, "Ignored failed webhook for a completed payment", reason));

                return payment.WithEvent(new PaymentEvent(at, "Payment failed", reason)) with
                {
                    Status = PaymentStatus.Failed,
                    StatusDetail = reason,
                };

            case PushPaymentEvents.Reversed:
                return payment.WithEvent(new PaymentEvent(at, "Payment reversed",
                    $"Transaction {payload.TransactionReferenceNumber} was reversed by the provider."))
                    with
                    {
                        Status = PaymentStatus.Reversed,
                        StatusDetail = "Reversed by the mobile money operator.",
                        TransactionReferenceNumber = payload.TransactionReferenceNumber ?? payment.TransactionReferenceNumber,
                    };

            default:
                return payment.WithEvent(new PaymentEvent(at, $"Ignored {webhook.EventName} webhook"));
        }
    }
}
