using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using OneKhusa.Client.Common;

namespace OneKhusa.Client.PushPayments;

internal sealed class PushPaymentClient(HttpClient httpClient, ILogger<PushPaymentClient> logger) : IPushPaymentClient
{
    internal const string IdempotencyKeyHeader = "X-Idempotency-Key";

    public async Task<OneKhusaResult<PushPaymentResponse>> InitiateAsync(
        PushPaymentRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        // Catch obvious mistakes locally instead of spending a round trip on them.
        var validationErrors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationErrors, validateAllProperties: true))
        {
            var message = string.Join(" ", validationErrors.Select(e => e.ErrorMessage));
            return OneKhusaResult<PushPaymentResponse>.Failure(new OneKhusaError($"Invalid push payment request: {message}"));
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "collections/push/direct/initiate")
        {
            Content = JsonContent.Create(request, options: OneKhusaHttp.JsonOptions),
        };
        httpRequest.Headers.Add(IdempotencyKeyHeader, idempotencyKey ?? Guid.NewGuid().ToString("N"));

        var result = await OneKhusaHttp.SendAsync<PushPaymentResponse>(httpClient, httpRequest, logger, cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Push payment {Reference} initiated with response code {ResponseCode}, amount to pay {AmountToPay}",
                result.Value.SourceReferenceNumber, result.Value.ResponseCode, result.Value.AmountToPay);
        }

        return result;
    }
}
