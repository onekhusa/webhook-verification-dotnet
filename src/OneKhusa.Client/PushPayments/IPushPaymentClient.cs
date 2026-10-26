using OneKhusa.Client.Common;

namespace OneKhusa.Client.PushPayments;

public interface IPushPaymentClient
{
    /// <summary>
    /// <c>POST /collections/push/direct/initiate</c>: sends a payment prompt to the customer's mobile
    /// money wallet. The outcome arrives later as a <c>pushpayment.*</c> webhook.
    /// </summary>
    /// <param name="idempotencyKey">
    /// Sent as <c>X-Idempotency-Key</c>. Pass the same key to safely retry a request whose outcome you
    /// did not receive; leave it null to generate a new one.
    /// </param>
    Task<OneKhusaResult<PushPaymentResponse>> InitiateAsync(
        PushPaymentRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);
}
