namespace OneKhusa.PushPayments.Web.Shared.Payments;

/// <summary>The payment store is shared: the initiate slice writes to it, the webhook slice updates it, the payments slice lists it.</summary>
public static class PaymentStoreRegistration
{
    public static IServiceCollection AddPaymentStore(this IServiceCollection services) =>
        services.AddSingleton<IPaymentStore, InMemoryPaymentStore>();
}
