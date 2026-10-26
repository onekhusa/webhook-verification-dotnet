namespace OneKhusa.PushPayments.Web.Features.InitiatePushPayment;

/// <summary>
/// Slice: send a payment prompt to a customer's mobile money wallet (README Step 5).
/// UI: <c>PushPaymentCard.razor</c>. Depends on <c>OneKhusa.Client</c> and the shared payment store.
/// </summary>
public static class InitiatePushPaymentFeature
{
    public static IServiceCollection AddInitiatePushPayment(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DemoOptions>(configuration.GetSection(DemoOptions.SectionName));
        services.AddScoped<InitiatePushPaymentHandler>();
        return services;
    }
}
