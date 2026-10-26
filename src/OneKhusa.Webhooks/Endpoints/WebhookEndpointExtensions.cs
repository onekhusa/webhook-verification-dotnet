using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace OneKhusa.Webhooks.Endpoints;

public static class WebhookEndpointExtensions
{
    public const string DefaultPushPaymentPattern = "/webhooks/onekhusa/pushpayment";

    /// <summary>
    /// Maps the push payment webhook receiver. Register this URL in the OneKhusa portal for the
    /// <c>pushpayment.success</c>, <c>pushpayment.failed</c> and <c>pushpayment.reversed</c> events.
    /// </summary>
    public static RouteHandlerBuilder MapOneKhusaPushPaymentWebhook(
        this IEndpointRouteBuilder endpoints,
        string pattern = DefaultPushPaymentPattern)
    {
        // Server-to-server call authenticated by its signature, so antiforgery does not apply.
        return endpoints.MapPost(pattern, PushPaymentWebhookEndpoint.HandleAsync)
            .DisableAntiforgery()
            .WithName("OneKhusaPushPaymentWebhook");
    }
}
