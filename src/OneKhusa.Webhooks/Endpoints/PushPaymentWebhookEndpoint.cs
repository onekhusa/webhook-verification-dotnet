using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneKhusa.Webhooks.Configuration;
using OneKhusa.Webhooks.Diagnostics;
using OneKhusa.Webhooks.Handling;
using OneKhusa.Webhooks.Models;
using OneKhusa.Webhooks.Verification;

namespace OneKhusa.Webhooks.Endpoints;

/// <summary>
/// Receives push payment webhooks. The order of the steps matters:
/// read raw body → verify signature → parse → deduplicate → handle → acknowledge.
/// </summary>
internal static class PushPaymentWebhookEndpoint
{
    public static async Task<IResult> HandleAsync(
        HttpContext context,
        IWebhookSignatureVerifier verifier,
        IWebhookDeduplicator deduplicator,
        IPushPaymentWebhookHandler handler,
        IEnumerable<IWebhookDeliveryObserver> observers,
        IOptions<OneKhusaWebhookOptions> options,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(typeof(PushPaymentWebhookEndpoint));
        var cancellationToken = context.RequestAborted;

        // OneKhusa needs the real status code. Status code pages (e.g. UseStatusCodePagesWithReExecute
        // in the Blazor template) would otherwise replace a 401 with an HTML error page.
        if (context.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
            statusCodePages.Enabled = false;
        var receivedAt = timeProvider.GetUtcNow();

        var eventName = ReadHeader(context.Request, WebhookHeaders.Event) ?? ReadHeader(context.Request, WebhookHeaders.EventAlternate);
        var signature = ReadHeader(context.Request, WebhookHeaders.Signature);

        // 1. Read the body exactly as sent. The signature covers these bytes, so verify before parsing:
        //    re-serialising parsed JSON almost never reproduces the same bytes.
        var rawBody = await ReadBodyAsync(context.Request, options.Value.MaxBodyBytes, cancellationToken);
        if (rawBody is null)
        {
            logger.LogWarning("Rejected OneKhusa webhook {Event}: body exceeds {MaxBytes} bytes", eventName, options.Value.MaxBodyBytes);
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var bodyText = Encoding.UTF8.GetString(rawBody);
        var headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        IResult Respond(WebhookVerificationResult verification, int statusCode, string outcome)
        {
            Notify(observers, new WebhookDelivery(receivedAt, eventName, signature, bodyText, headers, verification, statusCode, outcome), logger);
            return statusCode == StatusCodes.Status200OK
                ? Results.Ok(new { received = true, outcome })
                : Results.StatusCode(statusCode);
        }

        // 2. Verify the signature with the secret for this event.
        var verification = verifier.Verify(eventName, signature, rawBody);
        if (!verification.IsValid)
        {
            logger.LogWarning("Rejected OneKhusa webhook {Event}: {Reason}", eventName, verification.Status);
            return Respond(verification, StatusCodes.Status401Unauthorized, "Rejected");
        }

        // A verified event this endpoint does not handle (e.g. a different product's webhook pointed here).
        if (!PushPaymentEvents.IsPushPaymentEvent(eventName))
        {
            logger.LogInformation("Ignored verified OneKhusa webhook {Event}: not a push payment event", eventName);
            return Respond(verification, StatusCodes.Status200OK, "Ignored");
        }

        // 3. Only now is the body trusted enough to parse.
        PushPaymentWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PushPaymentWebhookPayload>(rawBody);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Rejected OneKhusa webhook {Event}: body is not valid JSON", eventName);
            payload = null;
        }

        if (payload is null)
            return Respond(verification, StatusCodes.Status400BadRequest, "Invalid payload");

        // 4. A redelivery has a byte-identical body and therefore the same signature.
        var deliveryKey = $"{eventName}:{signature}".ToLowerInvariant();
        if (await deduplicator.HasBeenProcessedAsync(deliveryKey, cancellationToken))
        {
            logger.LogInformation("Duplicate OneKhusa webhook {Event} for {Reference} acknowledged", eventName, payload.SourceReferenceNumber ?? payload.MerchantReferenceNumber);
            return Respond(verification, StatusCodes.Status200OK, "Duplicate");
        }

        // 5. Hand over to the application. Exceptions propagate (500) so the delivery is not marked processed.
        try
        {
            await handler.HandleAsync(new PushPaymentWebhook(eventName!.ToLowerInvariant(), payload, receivedAt), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Handler failed for OneKhusa webhook {Event}", eventName);
            Notify(observers, new WebhookDelivery(receivedAt, eventName, signature, bodyText, headers, verification, StatusCodes.Status500InternalServerError, "Handler failed"), logger);
            throw;
        }

        await deduplicator.MarkProcessedAsync(deliveryKey, cancellationToken);

        logger.LogInformation("Processed OneKhusa webhook {Event} for {Reference}", eventName, payload.SourceReferenceNumber ?? payload.MerchantReferenceNumber);
        return Respond(verification, StatusCodes.Status200OK, "Processed");
    }

    private static string? ReadHeader(HttpRequest request, string name)
    {
        var value = request.Headers[name].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <returns>The body, or <c>null</c> when it is larger than <paramref name="maxBytes"/>.</returns>
    private static async Task<byte[]?> ReadBodyAsync(HttpRequest request, int maxBytes, CancellationToken cancellationToken)
    {
        if (request.ContentLength > maxBytes)
            return null;

        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > maxBytes)
                return null;
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private static void Notify(IEnumerable<IWebhookDeliveryObserver> observers, WebhookDelivery delivery, ILogger logger)
    {
        foreach (var observer in observers)
        {
            try
            {
                observer.OnDelivery(delivery);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Webhook delivery observer {Observer} failed", observer.GetType().Name);
            }
        }
    }
}
