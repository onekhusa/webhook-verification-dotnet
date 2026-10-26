namespace OneKhusa.Webhooks.Verification;

public interface IWebhookSignatureVerifier
{
    /// <param name="eventName">Value of the event header.</param>
    /// <param name="signature">Value of the <see cref="WebhookHeaders.Signature"/> header.</param>
    /// <param name="rawBody">The request body exactly as received, before any JSON parsing.</param>
    WebhookVerificationResult Verify(string? eventName, string? signature, ReadOnlySpan<byte> rawBody);
}
