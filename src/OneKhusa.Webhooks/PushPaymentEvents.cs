namespace OneKhusa.Webhooks;

/// <summary>
/// Push payment webhook event codes. Each event has its own webhook secret in the OneKhusa portal
/// (Developers → Webhooks → Manage → View).
/// </summary>
public static class PushPaymentEvents
{
    /// <summary>The customer authorised the payment and the funds were collected. Fulfil the order.</summary>
    public const string Success = "pushpayment.success";

    /// <summary>No funds were collected: cancelled, wrong PIN, timed out or insufficient funds.</summary>
    public const string Failed = "pushpayment.failed";

    /// <summary>A previously successful payment was reversed and debited from the merchant balance.</summary>
    public const string Reversed = "pushpayment.reversed";

    public static IReadOnlyList<string> All { get; } = [Success, Failed, Reversed];

    public static bool IsPushPaymentEvent(string? eventName) =>
        eventName is not null && All.Contains(eventName, StringComparer.OrdinalIgnoreCase);
}
