using System.Text;
using OneKhusa.Webhooks.Verification;

namespace OneKhusa.Webhooks.Tests;

public class HmacSha512SignatureVerifierTests
{
    private const string SuccessSecret = "whsec_test_success";
    private const string FailedSecret = "whsec_test_failed";
    private const string Body = """{"connectorId":550044,"transactionStatusCode":"S"}""";

    // Computed independently: printf '%s' "$Body" | openssl dgst -sha512 -hmac whsec_test_success
    private const string KnownSignature =
        "6ba4a65b3e5aa5041d9d87f9c6b036ff75201897b88955698a3a35843fb76b9fcc1d7b970a77802e04a0e06628d61a8124c8b791ae30115b7a062512e81a6ecf";

    private readonly HmacSha512SignatureVerifier _verifier = new(new FakeSecretProvider(new()
    {
        [PushPaymentEvents.Success] = SuccessSecret,
        [PushPaymentEvents.Failed] = FailedSecret,
    }));

    private static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);

    [Fact]
    public void ComputeSignature_matches_an_independently_computed_vector()
    {
        Assert.Equal(KnownSignature, HmacSha512SignatureVerifier.ComputeSignature(SuccessSecret, Bytes(Body)));
    }

    [Fact]
    public void Valid_signature_is_accepted()
    {
        var result = _verifier.Verify(PushPaymentEvents.Success, KnownSignature, Bytes(Body));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Uppercase_hex_signature_is_accepted()
    {
        var result = _verifier.Verify(PushPaymentEvents.Success, KnownSignature.ToUpperInvariant(), Bytes(Body));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Tampered_body_is_rejected()
    {
        var tampered = Body.Replace("550044", "550045");

        var result = _verifier.Verify(PushPaymentEvents.Success, KnownSignature, Bytes(tampered));

        Assert.Equal(WebhookVerificationStatus.SignatureMismatch, result.Status);
    }

    [Fact]
    public void Whitespace_change_in_body_is_rejected()
    {
        // Why the raw bytes matter: the same JSON, re-formatted, no longer matches.
        var reformatted = """{ "connectorId": 550044, "transactionStatusCode": "S" }""";

        var result = _verifier.Verify(PushPaymentEvents.Success, KnownSignature, Bytes(reformatted));

        Assert.Equal(WebhookVerificationStatus.SignatureMismatch, result.Status);
    }

    [Fact]
    public void Signature_made_with_another_events_secret_is_rejected()
    {
        // Signed with the success secret, but delivered as a failed event.
        var result = _verifier.Verify(PushPaymentEvents.Failed, KnownSignature, Bytes(Body));

        Assert.Equal(WebhookVerificationStatus.SignatureMismatch, result.Status);
    }

    [Fact]
    public void Event_without_a_configured_secret_is_rejected()
    {
        var result = _verifier.Verify(PushPaymentEvents.Reversed, KnownSignature, Bytes(Body));

        Assert.Equal(WebhookVerificationStatus.NoSecretConfigured, result.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_signature_is_rejected(string? signature)
    {
        var result = _verifier.Verify(PushPaymentEvents.Success, signature, Bytes(Body));

        Assert.Equal(WebhookVerificationStatus.MissingSignatureHeader, result.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Missing_event_is_rejected(string? eventName)
    {
        var result = _verifier.Verify(eventName, KnownSignature, Bytes(Body));

        Assert.Equal(WebhookVerificationStatus.MissingEventHeader, result.Status);
    }

    [Theory]
    [InlineData("not-hex")]
    [InlineData("abc")]
    public void Non_hex_signature_is_rejected(string signature)
    {
        var result = _verifier.Verify(PushPaymentEvents.Success, signature, Bytes(Body));

        Assert.Equal(WebhookVerificationStatus.InvalidSignatureFormat, result.Status);
    }

    [Fact]
    public void Truncated_signature_is_rejected()
    {
        var result = _verifier.Verify(PushPaymentEvents.Success, KnownSignature[..64], Bytes(Body));

        Assert.Equal(WebhookVerificationStatus.SignatureMismatch, result.Status);
    }

    private sealed class FakeSecretProvider(Dictionary<string, string> secrets) : IWebhookSecretProvider
    {
        public string? GetSecret(string eventName) => secrets.GetValueOrDefault(eventName);
    }
}
