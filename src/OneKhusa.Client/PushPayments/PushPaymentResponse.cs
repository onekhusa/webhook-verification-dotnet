using System.Text.Json.Serialization;

namespace OneKhusa.Client.PushPayments;

/// <summary>
/// Confirms the payment prompt was sent to the customer's phone. It does <b>not</b> mean the customer
/// has paid: wait for the <c>pushpayment.success</c> webhook before fulfilling the order.
/// </summary>
public sealed record PushPaymentResponse
{
    [JsonPropertyName("merchantAccountNumber")]
    public int MerchantAccountNumber { get; init; }

    [JsonPropertyName("sourceReferenceNumber")]
    public string SourceReferenceNumber { get; init; } = string.Empty;

    /// <summary>"I100" when the prompt was dispatched. See <see cref="PushPaymentResponseCodes"/>.</summary>
    [JsonPropertyName("responseCode")]
    public string ResponseCode { get; init; } = string.Empty;

    /// <summary>Who pays the fees. See <see cref="FeeRuleCodes"/>.</summary>
    [JsonPropertyName("feeRuleCode")]
    public string? FeeRuleCode { get; init; }

    [JsonPropertyName("transactionAmount")]
    public decimal TransactionAmount { get; init; }

    /// <summary>Processing and gateway fees.</summary>
    [JsonPropertyName("totalCharges")]
    public decimal TotalCharges { get; init; }

    /// <summary>What the customer is asked to pay, including fees charged to them.</summary>
    [JsonPropertyName("amountToPay")]
    public decimal AmountToPay { get; init; }

    [JsonIgnore]
    public bool IsInitiated => ResponseCode == PushPaymentResponseCodes.Initiated;
}

public static class PushPaymentResponseCodes
{
    /// <summary>The prompt was sent to the customer's phone.</summary>
    public const string Initiated = "I100";
}

public static class FeeRuleCodes
{
    /// <summary>Fees are added to the amount the customer pays.</summary>
    public const string ChargeCustomer = "CC";

    /// <summary>Fees are deducted from what the merchant receives.</summary>
    public const string ChargeMerchant = "CM";
}
