using System.Text.Json.Serialization;

namespace OneKhusa.Client.Connectors;

/// <summary>A payment provider (mobile money operator or bank) available through OneKhusa.</summary>
public sealed record Connector
{
    /// <summary>The 6-digit id to send as <c>connectorId</c> when initiating a push payment.</summary>
    [JsonPropertyName("connectorId")]
    public int ConnectorId { get; init; }

    [JsonPropertyName("connectorName")]
    public string ConnectorName { get; init; } = string.Empty;

    [JsonPropertyName("connectorCode")]
    public string? ConnectorCode { get; init; }

    [JsonPropertyName("connectorDescription")]
    public string? ConnectorDescription { get; init; }

    /// <summary>"M" for mobile network operators, "B" for banks.</summary>
    [JsonPropertyName("channelCode")]
    public string? ChannelCode { get; init; }

    [JsonPropertyName("channelName")]
    public string? ChannelName { get; init; }

    [JsonPropertyName("switchMerchantId")]
    public string? SwitchMerchantId { get; init; }

    [JsonPropertyName("logoBase64")]
    public string? LogoBase64 { get; init; }

    /// <summary>Either a MIME type ("image/png") or a short format name ("PNG").</summary>
    [JsonPropertyName("logoFormat")]
    public string? LogoFormat { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    /// <summary>Mobile money connectors are the ones that can receive a push payment prompt.</summary>
    [JsonIgnore]
    public bool IsMobileMoney => string.Equals(ChannelCode, ChannelCodes.MobileNetworkOperator, StringComparison.OrdinalIgnoreCase);

    /// <summary>The logo as a <c>data:</c> URI for an <c>img</c> tag, or <c>null</c> when there is none.</summary>
    [JsonIgnore]
    public string? LogoDataUri
    {
        get
        {
            if (string.IsNullOrWhiteSpace(LogoBase64))
                return null;

            var mimeType = LogoFormat switch
            {
                null or "" => "image/png",
                var format when format.Contains('/') => format,
                var format => "image/" + format.ToLowerInvariant().Replace("jpg", "jpeg"),
            };
            return $"data:{mimeType};base64,{LogoBase64}";
        }
    }
}

public static class ChannelCodes
{
    public const string MobileNetworkOperator = "M";
    public const string Bank = "B";
}
