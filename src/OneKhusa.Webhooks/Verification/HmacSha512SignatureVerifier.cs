using System.Security.Cryptography;
using System.Text;

namespace OneKhusa.Webhooks.Verification;

/// <summary>
/// Verifies OneKhusa webhook signatures: HMAC-SHA512 over the raw request body, keyed with the
/// secret of the event being delivered, hex encoded, compared in constant time.
/// </summary>
public sealed class HmacSha512SignatureVerifier(IWebhookSecretProvider secretProvider) : IWebhookSignatureVerifier
{
    public WebhookVerificationResult Verify(string? eventName, string? signature, ReadOnlySpan<byte> rawBody)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            return new(WebhookVerificationStatus.MissingEventHeader);

        if (string.IsNullOrWhiteSpace(signature))
            return new(WebhookVerificationStatus.MissingSignatureHeader);

        // Fail closed: without a secret there is no way to tell a genuine delivery from a forged one.
        var secret = secretProvider.GetSecret(eventName.Trim());
        if (string.IsNullOrEmpty(secret))
            return new(WebhookVerificationStatus.NoSecretConfigured);

        byte[] providedSignature;
        try
        {
            // Decoding the hex makes the comparison case-insensitive without extra string handling.
            providedSignature = Convert.FromHexString(signature.Trim());
        }
        catch (FormatException)
        {
            return new(WebhookVerificationStatus.InvalidSignatureFormat);
        }

        var expectedSignature = HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), rawBody);

        return CryptographicOperations.FixedTimeEquals(expectedSignature, providedSignature)
            ? WebhookVerificationResult.Valid
            : new(WebhookVerificationStatus.SignatureMismatch);
    }

    /// <summary>
    /// Computes the signature OneKhusa would send for <paramref name="rawBody"/>. Useful for tests and
    /// for simulating deliveries locally.
    /// </summary>
    public static string ComputeSignature(string secret, ReadOnlySpan<byte> rawBody) =>
        Convert.ToHexStringLower(HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), rawBody));
}
