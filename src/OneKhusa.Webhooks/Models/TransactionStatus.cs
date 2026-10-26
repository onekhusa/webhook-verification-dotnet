namespace OneKhusa.Webhooks.Models;

/// <summary>Meaning of the webhook's <c>transactionStatusCode</c>.</summary>
public enum TransactionStatus
{
    Unknown,
    Success,
    Failed,
    Reversed,
}
