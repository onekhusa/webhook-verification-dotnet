namespace OneKhusa.Webhooks.Configuration;

public sealed class OneKhusaWebhookOptions
{
    public const string SectionName = "OneKhusa:Webhooks";

    /// <summary>
    /// Webhook secret per event code, for example
    /// <c>"pushpayment.success": "&lt;secret from the portal&gt;"</c>. Keep these out of source control.
    /// </summary>
    public Dictionary<string, string> Secrets { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Requests with a larger body are rejected before verification. Defaults to 64 KB.</summary>
    public int MaxBodyBytes { get; set; } = 64 * 1024;
}
