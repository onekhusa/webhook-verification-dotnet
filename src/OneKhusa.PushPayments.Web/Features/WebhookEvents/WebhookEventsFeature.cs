using OneKhusa.Webhooks.Diagnostics;

namespace OneKhusa.PushPayments.Web.Features.WebhookEvents;

/// <summary>
/// Slice: show every webhook delivery, accepted or rejected, and send signed test webhooks
/// (README Step 10). UI: <c>WebhookEventsCard.razor</c>. Diagnostics only, not needed in production.
/// </summary>
public static class WebhookEventsFeature
{
    public static IServiceCollection AddWebhookEvents(this IServiceCollection services)
    {
        services.AddSingleton<WebhookLog>();
        services.AddSingleton<IWebhookDeliveryObserver>(provider => provider.GetRequiredService<WebhookLog>());
        services.AddHttpClient(WebhookSimulator.HttpClientName);
        services.AddScoped<WebhookSimulator>();
        return services;
    }
}
