using System.Text.Json.Serialization;

namespace OneKhusa.Webhooks.Models;

/// <summary>
/// Body of a <c>pushpayment.*</c> webhook. Some fields are only present on some events, for example
/// <see cref="ErrorMessages"/> on failures and <see cref="TransactionReferenceNumber"/> on success and reversal.
/// </summary>
public sealed record PushPaymentWebhookPayload
{
    /// <summary>The mobile money provider the payment went through.</summary>
    [JsonPropertyName("connectorId")]
    public int ConnectorId { get; init; }

    /// <summary>The payer's wallet number (MSISDN).</summary>
    [JsonPropertyName("sourceAccountNumber")]
    public string? SourceAccountNumber { get; init; }

    [JsonPropertyName("sourceAccountName")]
    public string? SourceAccountName { get; init; }

    /// <summary>The payer's provider, for example "Airtel Money" or "TNM MPAMBA".</summary>
    [JsonPropertyName("sourceInstitution")]
    public string? SourceInstitution { get; init; }

    [JsonPropertyName("sourceReferenceNumber")]
    public string? SourceReferenceNumber { get; init; }

    [JsonPropertyName("merchantReferenceNumber")]
    public string? MerchantReferenceNumber { get; init; }

    /// <summary>Your merchant account number.</summary>
    [JsonPropertyName("beneficiaryAccountNumber")]
    public string? BeneficiaryAccountNumber { get; init; }

    /// <summary>OneKhusa's reference for the settled transaction.</summary>
    [JsonPropertyName("transactionReferenceNumber")]
    public string? TransactionReferenceNumber { get; init; }

    [JsonPropertyName("transactionDescription")]
    public string? TransactionDescription { get; init; }

    [JsonPropertyName("transactionAmount")]
    public decimal TransactionAmount { get; init; }

    [JsonPropertyName("transactionFee")]
    public decimal? TransactionFee { get; init; }

    [JsonPropertyName("transactionDate")]
    public DateTimeOffset? TransactionDate { get; init; }

    /// <summary>S = success, F = failed, R = reversed. See <see cref="Status"/>.</summary>
    [JsonPropertyName("transactionStatusCode")]
    public string? TransactionStatusCode { get; init; }

    /// <summary>Transaction type code, for example "DMP".</summary>
    [JsonPropertyName("transactionCode")]
    public string? TransactionCode { get; init; }

    /// <summary>For example "S100" on success or "E307" on a failure.</summary>
    [JsonPropertyName("responseCode")]
    public string? ResponseCode { get; init; }

    [JsonPropertyName("errorMessages")]
    public IReadOnlyList<string>? ErrorMessages { get; init; }

    [JsonIgnore]
    public TransactionStatus Status => TransactionStatusCode?.Trim().ToUpperInvariant() switch
    {
        "S" => TransactionStatus.Success,
        "F" => TransactionStatus.Failed,
        "R" => TransactionStatus.Reversed,
        _ => TransactionStatus.Unknown,
    };
}
