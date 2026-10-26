namespace OneKhusa.PushPayments.Web.Payments;

/// <summary>Demo storage: lost on restart. Replace with a database in a real integration.</summary>
public sealed class InMemoryPaymentStore : IPaymentStore
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, PaymentRecord> _payments = new(StringComparer.OrdinalIgnoreCase);

    public event Action? Changed;

    public void Add(PaymentRecord payment)
    {
        lock (_lock)
        {
            if (!_payments.TryAdd(payment.SourceReferenceNumber, payment))
                throw new InvalidOperationException($"Payment {payment.SourceReferenceNumber} already exists.");
        }

        Changed?.Invoke();
    }

    public PaymentRecord? Find(string sourceReferenceNumber)
    {
        lock (_lock)
        {
            return _payments.GetValueOrDefault(sourceReferenceNumber);
        }
    }

    public IReadOnlyList<PaymentRecord> GetAll()
    {
        lock (_lock)
        {
            return _payments.Values.OrderByDescending(p => p.CreatedAt).ToList();
        }
    }

    public PaymentRecord? Update(string sourceReferenceNumber, Func<PaymentRecord, PaymentRecord> update)
    {
        PaymentRecord updated;
        lock (_lock)
        {
            if (!_payments.TryGetValue(sourceReferenceNumber, out var current))
                return null;

            updated = update(current);
            _payments[sourceReferenceNumber] = updated;
        }

        Changed?.Invoke();
        return updated;
    }
}
