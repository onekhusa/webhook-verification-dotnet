using Microsoft.Extensions.Caching.Memory;

namespace OneKhusa.Webhooks.Handling;

/// <summary>
/// Single-instance deduplication backed by <see cref="IMemoryCache"/>. If you run more than one
/// instance, replace it with one backed by your database or a distributed cache.
/// </summary>
public sealed class InMemoryWebhookDeduplicator(IMemoryCache cache) : IWebhookDeduplicator
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    public ValueTask<bool> HasBeenProcessedAsync(string deliveryKey, CancellationToken cancellationToken) =>
        ValueTask.FromResult(cache.TryGetValue(CacheKey(deliveryKey), out _));

    public ValueTask MarkProcessedAsync(string deliveryKey, CancellationToken cancellationToken)
    {
        cache.Set(CacheKey(deliveryKey), true, Retention);
        return ValueTask.CompletedTask;
    }

    private static string CacheKey(string deliveryKey) => $"onekhusa:webhook:{deliveryKey}";
}
