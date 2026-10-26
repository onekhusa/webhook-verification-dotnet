using OneKhusa.Webhooks.DependencyInjection;
using OneKhusa.Webhooks.Endpoints;
using OneKhusa.Webhooks.Handling;

namespace OneKhusa.PushPayments.Web.Features.ReceivePushPaymentWebhook;

/// <summary>
/// Slice: receive, verify and apply <c>pushpayment.*</c> webhooks (README Steps 6–9).
/// Verification comes from <c>OneKhusa.Webhooks</c>; what each event means for a payment is
/// <see cref="PushPaymentWebhookHandler"/>.
/// </summary>
public static class ReceivePushPaymentWebhookFeature
{
    public static IServiceCollection AddReceivePushPaymentWebhook(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOneKhusaWebhooks(configuration);
        services.AddScoped<IPushPaymentWebhookHandler, PushPaymentWebhookHandler>();
        return services;
    }

    /// <summary>Maps <c>POST /webhooks/onekhusa/pushpayment</c>. Register this URL in the OneKhusa portal.</summary>
    public static IEndpointRouteBuilder MapReceivePushPaymentWebhook(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOneKhusaPushPaymentWebhook();
        return endpoints;
    }
}
