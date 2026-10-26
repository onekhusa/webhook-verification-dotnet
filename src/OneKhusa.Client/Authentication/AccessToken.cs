namespace OneKhusa.Client.Authentication;

/// <summary>A bearer token for one organisation and merchant account. Valid for 5 minutes.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresOn)
{
    // Keep the token out of logs and debugger displays.
    public override string ToString() => $"AccessToken (expires {ExpiresOn:O})";
}
