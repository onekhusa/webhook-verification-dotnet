using System.Net;
using System.Text.Json;

namespace OneKhusa.Client.Common;

/// <param name="Message">A readable summary, safe to show to an operator.</param>
/// <param name="StatusCode">The HTTP status, when OneKhusa responded.</param>
/// <param name="ResponseBody">The raw response body, for troubleshooting.</param>
public sealed record OneKhusaError(string Message, HttpStatusCode? StatusCode = null, string? ResponseBody = null)
{
    internal static OneKhusaError FromResponse(HttpStatusCode statusCode, string body)
    {
        var detail = TryReadMessages(body);
        var message = detail is null
            ? $"OneKhusa returned HTTP {(int)statusCode} ({statusCode})."
            : $"OneKhusa returned HTTP {(int)statusCode}: {detail}";
        return new OneKhusaError(message, statusCode, body);
    }

    /// <summary>
    /// Errors come back as problem details, e.g.
    /// <c>{"title":"Validation Error","detail":"Failed to retrieve access token","errorCode":"E900"}</c>.
    /// Other common message fields are read as a fallback.
    /// </summary>
    private static string? TryReadMessages(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return null;

            string? message = null;
            if (root.TryGetProperty("errorMessages", out var messages) && messages.ValueKind == JsonValueKind.Array)
                message = string.Join("; ", messages.EnumerateArray().Select(e => e.ToString()));

            foreach (var name in new[] { "detail", "message", "errorMessage", "title" })
            {
                if (!string.IsNullOrEmpty(message))
                    break;
                if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                    message = value.GetString();
            }

            if (root.TryGetProperty("errorCode", out var code) && code.ValueKind == JsonValueKind.String)
                message = string.IsNullOrEmpty(message) ? code.GetString() : $"{message} ({code.GetString()})";

            return message;
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
