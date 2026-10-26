namespace OneKhusa.Client.Configuration;

/// <summary>Credentials and endpoint for the OneKhusa API, bound from the <c>OneKhusa</c> configuration section.</summary>
public sealed class OneKhusaOptions
{
    public const string SectionName = "OneKhusa";

    /// <summary>Sandbox by default. Use the production URL from OneKhusa once your KYC is approved.</summary>
    public string BaseUrl { get; set; } = "https://api.onekhusa.com/sandbox/v1";

    /// <summary>Portal → Developers → API Keys.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Server side only. Never ship it to a browser or mobile app.</summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Portal → Settings → Profile.</summary>
    public string OrganisationId { get; set; } = string.Empty;

    /// <summary>The 8-digit merchant account number (Portal → Merchants). Access tokens are bound to it.</summary>
    public int MerchantAccountNumber { get; set; }

    /// <summary>Sent as <c>Accept-Language</c>.</summary>
    public string Language { get; set; } = "en";

    /// <summary>Lists what is missing or invalid. An empty list means the client is ready to call the API.</summary>
    public IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            errors.Add($"{SectionName}:BaseUrl must be an absolute https URL.");
        if (string.IsNullOrWhiteSpace(ApiKey))
            errors.Add($"{SectionName}:ApiKey is required.");
        if (string.IsNullOrWhiteSpace(ApiSecret))
            errors.Add($"{SectionName}:ApiSecret is required.");
        if (string.IsNullOrWhiteSpace(OrganisationId))
            errors.Add($"{SectionName}:OrganisationId is required.");
        if (MerchantAccountNumber is < 10_000_000 or > 99_999_999)
            errors.Add($"{SectionName}:MerchantAccountNumber must be your 8-digit merchant account number.");

        return errors;
    }
}
