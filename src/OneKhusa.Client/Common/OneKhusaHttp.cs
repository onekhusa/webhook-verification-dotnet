using System.Text.Json;
using Microsoft.Extensions.Logging;
using OneKhusa.Client.Authentication;

namespace OneKhusa.Client.Common;

/// <summary>Sends a request and turns the response into a <see cref="OneKhusaResult{T}"/>.</summary>
internal static class OneKhusaHttp
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<OneKhusaResult<T>> SendAsync<T>(
        HttpClient httpClient,
        HttpRequestMessage request,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var operation = $"{request.Method} {request.RequestUri}";
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (OneKhusaAuthenticationException ex)
        {
            return OneKhusaResult<T>.Failure(ex.Error);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "OneKhusa request {Operation} failed", operation);
            return OneKhusaResult<T>.Failure(new OneKhusaError($"Could not reach OneKhusa: {ex.Message}"));
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "OneKhusa request {Operation} timed out", operation);
            return OneKhusaResult<T>.Failure(new OneKhusaError("The request to OneKhusa timed out."));
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("OneKhusa request {Operation} returned {StatusCode}", operation, (int)response.StatusCode);
                logger.LogDebug("OneKhusa error body: {Body}", body);
                return OneKhusaResult<T>.Failure(OneKhusaError.FromResponse(response.StatusCode, body));
            }

            try
            {
                var value = JsonSerializer.Deserialize<T>(body, JsonOptions);
                if (value is not null)
                    return OneKhusaResult<T>.Success(value);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "OneKhusa request {Operation} returned an unexpected body", operation);
            }

            return OneKhusaResult<T>.Failure(new OneKhusaError("OneKhusa returned a response that could not be read.", response.StatusCode, body));
        }
    }
}
