using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneKhusa.Webhooks.DependencyInjection;
using OneKhusa.Webhooks.Verification;

namespace OneKhusa.Webhooks.Tests;

public class ConfigurationWebhookSecretProviderTests
{
    private static IWebhookSecretProvider CreateProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection()
            .AddOneKhusaWebhooks(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IWebhookSecretProvider>();
    }

    [Fact]
    public void Reads_a_secret_per_event()
    {
        var provider = CreateProvider(new()
        {
            ["OneKhusa:Webhooks:Secrets:pushpayment.success"] = "success-secret",
            ["OneKhusa:Webhooks:Secrets:pushpayment.failed"] = "failed-secret",
        });

        Assert.Equal("success-secret", provider.GetSecret(PushPaymentEvents.Success));
        Assert.Equal("failed-secret", provider.GetSecret(PushPaymentEvents.Failed));
    }

    [Fact]
    public void Event_lookup_is_case_insensitive()
    {
        var provider = CreateProvider(new() { ["OneKhusa:Webhooks:Secrets:pushpayment.success"] = "success-secret" });

        Assert.Equal("success-secret", provider.GetSecret("PushPayment.Success"));
    }

    [Fact]
    public void Blank_or_missing_secret_returns_null()
    {
        var provider = CreateProvider(new() { ["OneKhusa:Webhooks:Secrets:pushpayment.success"] = "" });

        Assert.Null(provider.GetSecret(PushPaymentEvents.Success));
        Assert.Null(provider.GetSecret(PushPaymentEvents.Reversed));
    }
}
