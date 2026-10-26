using System.Security.Cryptography;

namespace OneKhusa.PushPayments.Web.Features.InitiatePushPayment;

/// <summary>Generates the <c>sourceReferenceNumber</c> sent with each push request (OneKhusa allows 5–25 characters).</summary>
public static class SourceReference
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>For example <c>PP261026081530K7Q2XM</c>: prefix, UTC timestamp, random suffix (20 characters).</summary>
    public static string New(TimeProvider timeProvider) =>
        $"PP{timeProvider.GetUtcNow():yyMMddHHmmss}{RandomNumberGenerator.GetString(Alphabet, 6)}";
}
