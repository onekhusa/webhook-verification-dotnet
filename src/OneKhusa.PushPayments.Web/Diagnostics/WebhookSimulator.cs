using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OneKhusa.Client.Configuration;
using OneKhusa.PushPayments.Web.Payments;
using OneKhusa.Webhooks;
using OneKhusa.Webhooks.Verification;

namespace OneKhusa.PushPayments.Web.Diagnostics;

/// <summary>
/// Development helper: signs a sample payload the way OneKhusa does and POSTs it to this app's own
/// webhook endpoint, so the full receive → verify → handle path can be tried without the portal.
/// </summary>
public sealed class WebhookSimulator(
    IHttpClientFactory httpClientFactory,
    IWebhookSecretProvider secretProvider,
    IOptions<OneKhusaOptions> oneKhusaOptions,
    TimeProvider timeProvider)
{
    internal const string HttpClientName = "WebhookSimulator";

    private static readonly JsonSerializerOptions PrettyJson = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>A payload shaped like the examples on the Push Payment Webhook docs page.</summary>
    public string BuildSamplePayload(string eventName, PaymentRecord? payment)
    {
        var now = timeProvider.GetUtcNow();
        var reference = payment?.SourceReferenceNumber ?? "PP-UNKNOWN-REF";
        var amount = payment?.Amount ?? 100m;
        var common = new Dictionary<string, object?>
        {
            ["connectorId"] = payment?.ConnectorId ?? 550044,
            ["sourceAccountNumber"] = payment?.PayerAccountNumber ?? "265999000000",
            ["sourceInstitution"] = payment?.ConnectorName ?? "Airtel Money",
            ["merchantReferenceNumber"] = reference,
            ["beneficiaryAccountNumber"] = oneKhusaOptions.Value.MerchantAccountNumber.ToString(),
            ["transactionDescription"] = payment?.Description ?? "Sample order payment",
            ["transactionAmount"] = amount,
            ["transactionDate"] = now,
            ["transactionCode"] = "DMP",
        };

        if (eventName == PushPaymentEvents.Failed)
        {
            common["transactionStatusCode"] = "F";
            common["responseCode"] = "E307";
            common["errorMessages"] = new[] { "Customer cancelled the transaction" };
        }
        else
        {
            common["sourceAccountName"] = "Sandbox Customer";
            common["sourceReferenceNumber"] = $"MNO{now:yyMMddHHmmss}";
            common["transactionReferenceNumber"] = $"{now:yyMMdd}SIM{Random.Shared.Next(100000, 999999)}";
            common["transactionFee"] = Math.Round(amount * 0.0105m, 2);
            common["transactionStatusCode"] = eventName == PushPaymentEvents.Reversed ? "R" : "S";
            common["responseCode"] = "S100";
        }

        return JsonSerializer.Serialize(common, PrettyJson);
    }

    public async Task<SimulationResult> SendAsync(
        Uri endpoint,
        string eventName,
        string body,
        bool corruptSignature,
        CancellationToken cancellationToken = default)
    {
        var secret = secretProvider.GetSecret(eventName);
        if (secret is null)
            return SimulationResult.Failed($"Configure OneKhusa:Webhooks:Secrets:{eventName} before simulating this event.");

        var signature = HmacSha512SignatureVerifier.ComputeSignature(secret, Encoding.UTF8.GetBytes(body));
        if (corruptSignature)
            signature = (signature[0] == '0' ? '1' : '0') + signature[1..];

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(WebhookHeaders.Event, eventName);
        request.Headers.Add(WebhookHeaders.Signature, signature);

        try
        {
            using var response = await httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            return new SimulationResult((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken), signature, null);
        }
        catch (HttpRequestException ex)
        {
            return SimulationResult.Failed($"Could not call {endpoint}: {ex.Message}");
        }
    }
}

public sealed record SimulationResult(int? StatusCode, string? ResponseBody, string? Signature, string? Error)
{
    public static SimulationResult Failed(string error) => new(null, null, null, error);
}
