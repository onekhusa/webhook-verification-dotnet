using OneKhusa.Webhooks.Diagnostics;

namespace OneKhusa.PushPayments.Web.Diagnostics;

/// <summary>Keeps the most recent webhook deliveries in memory for the Webhook Inspector page.</summary>
public sealed class WebhookLog : IWebhookDeliveryObserver
{
    private const int Capacity = 100;

    private readonly Lock _lock = new();
    private readonly LinkedList<WebhookDelivery> _deliveries = new();

    public event Action? Changed;

    public void OnDelivery(WebhookDelivery delivery)
    {
        lock (_lock)
        {
            _deliveries.AddFirst(delivery);
            if (_deliveries.Count > Capacity)
                _deliveries.RemoveLast();
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        lock (_lock)
        {
            _deliveries.Clear();
        }

        Changed?.Invoke();
    }

    /// <summary>Newest first.</summary>
    public IReadOnlyList<WebhookDelivery> GetAll()
    {
        lock (_lock)
        {
            return _deliveries.ToList();
        }
    }

    /// <summary>Shows enough of a signature to compare by eye without displaying the whole value.</summary>
    public static string Abbreviate(string? signature) => signature switch
    {
        null or "" => "(none)",
        { Length: <= 16 } => signature,
        _ => $"{signature[..8]}…{signature[^8..]}",
    };
}
