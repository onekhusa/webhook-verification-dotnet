namespace OneKhusa.PushPayments.Web.Configuration;

/// <summary>Defaults for the demo UI. Not part of the OneKhusa integration itself.</summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>Pre-fills "Captured by". In a real app this is the signed-in user's email.</summary>
    public string CapturedBy { get; set; } = string.Empty;
}
