namespace OneKhusa.PushPayments.Web.Payments;

public enum PaymentStatus
{
    /// <summary>Saved locally, request to OneKhusa in flight.</summary>
    Initiating,

    /// <summary>OneKhusa sent the prompt; waiting for the customer to enter their PIN.</summary>
    AwaitingCustomer,

    /// <summary><c>pushpayment.success</c> received. The only status that should trigger fulfilment.</summary>
    Paid,

    /// <summary><c>pushpayment.failed</c> received. No money was collected.</summary>
    Failed,

    /// <summary><c>pushpayment.reversed</c> received after a successful payment.</summary>
    Reversed,

    /// <summary>OneKhusa did not accept the push request, so no prompt was sent.</summary>
    InitiationFailed,
}

/// <summary>The sample's view of one push payment, keyed by the <c>sourceReferenceNumber</c> we generated.</summary>
public sealed record PaymentRecord
{
    public required string SourceReferenceNumber { get; init; }

    public required int ConnectorId { get; init; }

    public string? ConnectorName { get; init; }

    public required string PayerAccountNumber { get; init; }

    public required string Description { get; init; }

    public required decimal Amount { get; init; }

    public decimal? TotalCharges { get; init; }

    public decimal? AmountToPay { get; init; }

    public string? FeeRuleCode { get; init; }

    public PaymentStatus Status { get; init; } = PaymentStatus.Initiating;

    /// <summary>Why the payment failed or was not initiated.</summary>
    public string? StatusDetail { get; init; }

    /// <summary>OneKhusa's reference, from the success or reversal webhook.</summary>
    public string? TransactionReferenceNumber { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public IReadOnlyList<PaymentEvent> History { get; init; } = [];

    public PaymentRecord WithEvent(PaymentEvent paymentEvent) =>
        this with { History = [.. History, paymentEvent], UpdatedAt = paymentEvent.At };
}

public sealed record PaymentEvent(DateTimeOffset At, string Title, string? Detail = null);
