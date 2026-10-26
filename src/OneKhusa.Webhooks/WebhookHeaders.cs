namespace OneKhusa.Webhooks;

/// <summary>HTTP headers OneKhusa sends with every webhook delivery.</summary>
public static class WebhookHeaders
{
    /// <summary>Lowercase hex HMAC-SHA512 of the raw request body, keyed with the event's webhook secret.</summary>
    public const string Signature = "X-OneKhusa-Webhook-Signature";

    /// <summary>The event code, for example <c>pushpayment.success</c>.</summary>
    public const string Event = "X-OneKhusa-Webhook-Event";

    /// <summary>
    /// The event header spelling used on the "Verify Webhook" docs page. Accepted as a fallback
    /// so the receiver keeps working whichever spelling is sent.
    /// </summary>
    public const string EventAlternate = "OneKhusa-Webhook-Event";
}
