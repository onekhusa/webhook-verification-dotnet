namespace OneKhusa.Webhooks.Verification;

public enum WebhookVerificationStatus
{
    Valid,
    MissingEventHeader,
    MissingSignatureHeader,
    NoSecretConfigured,
    InvalidSignatureFormat,
    SignatureMismatch,
}

public sealed record WebhookVerificationResult(WebhookVerificationStatus Status)
{
    public static WebhookVerificationResult Valid { get; } = new(WebhookVerificationStatus.Valid);

    public bool IsValid => Status == WebhookVerificationStatus.Valid;

    public string Description => Status switch
    {
        WebhookVerificationStatus.Valid => "Signature is valid.",
        WebhookVerificationStatus.MissingEventHeader => $"The {WebhookHeaders.Event} header is missing.",
        WebhookVerificationStatus.MissingSignatureHeader => $"The {WebhookHeaders.Signature} header is missing.",
        WebhookVerificationStatus.NoSecretConfigured => "No webhook secret is configured for this event, so the delivery cannot be trusted.",
        WebhookVerificationStatus.InvalidSignatureFormat => "The signature header is not a hex string.",
        WebhookVerificationStatus.SignatureMismatch => "The signature does not match the request body. Check the secret configured for this event.",
        _ => Status.ToString(),
    };
}
