namespace OneKhusa.Webhooks.Handling;

/// <summary>Remembers processed deliveries so a redelivered webhook is acknowledged without running the handler again.</summary>
public interface IWebhookDeduplicator
{
    ValueTask<bool> HasBeenProcessedAsync(string deliveryKey, CancellationToken cancellationToken);

    ValueTask MarkProcessedAsync(string deliveryKey, CancellationToken cancellationToken);
}
