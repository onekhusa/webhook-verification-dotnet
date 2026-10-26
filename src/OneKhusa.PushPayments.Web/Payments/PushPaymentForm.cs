using System.ComponentModel.DataAnnotations;

namespace OneKhusa.PushPayments.Web.Payments;

/// <summary>What the operator fills in on the push payment page.</summary>
public sealed class PushPaymentForm
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a mobile money provider.")]
    public int ConnectorId { get; set; }

    /// <summary>
    /// The wallet's MSISDN: digits only, optionally with a leading +. Live numbers look like
    /// 0999000000 or 265999000000; the sandbox only accepts its test wallets (221100000101, 231100000101).
    /// </summary>
    [Required(ErrorMessage = "Enter the customer's mobile money number.")]
    [RegularExpression(@"^\s*\+?\d{9,15}\s*$", ErrorMessage = "Enter digits only, e.g. 0999000000, or a sandbox test number such as 221100000101.")]
    public string PayerAccountNumber { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "10000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Enter an amount between 1 and 10,000,000.")]
    public decimal Amount { get; set; } = 100m;

    [Required(ErrorMessage = "Enter a description; the customer sees it on the prompt.")]
    [StringLength(100)]
    public string Description { get; set; } = "Sample order payment";

    [Required(ErrorMessage = "Enter the email of the person initiating the payment.")]
    [EmailAddress]
    public string CapturedBy { get; set; } = string.Empty;

    /// <summary>Optional. Leave empty to have one generated; it must be unique per payment.</summary>
    [RegularExpression(@"^\s*[A-Za-z0-9\-]{5,25}\s*$", ErrorMessage = "Use 5-25 letters, digits or dashes, or leave empty.")]
    public string? SourceReferenceNumber { get; set; }
}
