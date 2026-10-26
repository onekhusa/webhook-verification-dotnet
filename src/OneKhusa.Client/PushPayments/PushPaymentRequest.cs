using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OneKhusa.Client.PushPayments;

/// <summary>Body of <c>POST /collections/push/direct/initiate</c>.</summary>
public sealed record PushPaymentRequest
{
    /// <summary>Must be the merchant account the access token was issued for.</summary>
    [JsonPropertyName("merchantAccountNumber")]
    [Range(10_000_000, 99_999_999)]
    public required int MerchantAccountNumber { get; init; }

    /// <summary>The mobile money provider, from <see cref="Connectors.IConnectorsClient"/>.</summary>
    [JsonPropertyName("connectorId")]
    [Range(1, int.MaxValue)]
    public required int ConnectorId { get; init; }

    /// <summary>The payer's wallet number (MSISDN), for example "0999000000".</summary>
    [JsonPropertyName("payerAccountNumber")]
    [Required]
    public required string PayerAccountNumber { get; init; }

    /// <summary>Your own reference, unique per request. Webhooks for this payment refer back to it.</summary>
    [JsonPropertyName("sourceReferenceNumber")]
    [Required, StringLength(25, MinimumLength = 5)]
    public required string SourceReferenceNumber { get; init; }

    /// <summary>Shown to the customer on the payment prompt.</summary>
    [JsonPropertyName("transactionDescription")]
    [Required]
    public required string TransactionDescription { get; init; }

    [JsonPropertyName("transactionAmount")]
    [Range(typeof(decimal), "0.01", "1000000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public required decimal TransactionAmount { get; init; }

    /// <summary>Email of the user who initiated the payment, for OneKhusa's audit trail.</summary>
    [JsonPropertyName("capturedBy")]
    [Required, EmailAddress]
    public required string CapturedBy { get; init; }
}
