namespace OneKhusa.PushPayments.Web.Payments;

/// <summary>
/// Where the sample keeps payments. In your application this is your orders or payments table.
/// </summary>
public interface IPaymentStore
{
    /// <summary>Raised after any payment is added or changed, so open pages can refresh.</summary>
    event Action? Changed;

    void Add(PaymentRecord payment);

    PaymentRecord? Find(string sourceReferenceNumber);

    /// <summary>Newest first.</summary>
    IReadOnlyList<PaymentRecord> GetAll();

    /// <summary>Applies <paramref name="update"/> atomically. Returns the updated payment, or null if it does not exist.</summary>
    PaymentRecord? Update(string sourceReferenceNumber, Func<PaymentRecord, PaymentRecord> update);
}
