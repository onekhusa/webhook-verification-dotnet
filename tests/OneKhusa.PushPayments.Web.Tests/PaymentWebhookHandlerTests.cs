using OneKhusa.PushPayments.Web.Payments;
using OneKhusa.Webhooks;
using OneKhusa.Webhooks.Handling;
using OneKhusa.Webhooks.Models;

namespace OneKhusa.PushPayments.Web.Tests;

public class PaymentWebhookHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 8, 0, 0, TimeSpan.Zero);

    private static PaymentRecord Payment(PaymentStatus status) => new()
    {
        SourceReferenceNumber = "PPTEST",
        ConnectorId = 550044,
        PayerAccountNumber = "0999000000",
        Description = "Order",
        Amount = 100m,
        Status = status,
        CreatedAt = Now,
    };

    private static PushPaymentWebhook Webhook(string eventName) =>
        new(eventName, new PushPaymentWebhookPayload { TransactionReferenceNumber = "TXN1", ErrorMessages = ["Insufficient funds"] }, Now);

    [Theory]
    [InlineData(PaymentStatus.AwaitingCustomer, PushPaymentEvents.Success, PaymentStatus.Paid)]
    [InlineData(PaymentStatus.AwaitingCustomer, PushPaymentEvents.Failed, PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Paid, PushPaymentEvents.Reversed, PaymentStatus.Reversed)]
    [InlineData(PaymentStatus.Initiating, PushPaymentEvents.Success, PaymentStatus.Paid)]
    public void Applies_the_event(PaymentStatus current, string eventName, PaymentStatus expected)
    {
        var updated = PaymentWebhookHandler.Apply(Payment(current), Webhook(eventName));

        Assert.Equal(expected, updated.Status);
        Assert.Single(updated.History);
    }

    [Theory]
    [InlineData(PaymentStatus.Paid, PushPaymentEvents.Failed)]
    [InlineData(PaymentStatus.Reversed, PushPaymentEvents.Failed)]
    [InlineData(PaymentStatus.Reversed, PushPaymentEvents.Success)]
    public void Out_of_order_events_do_not_undo_a_completed_payment(PaymentStatus current, string eventName)
    {
        var updated = PaymentWebhookHandler.Apply(Payment(current), Webhook(eventName));

        Assert.Equal(current, updated.Status);
        Assert.StartsWith("Ignored", updated.History.Single().Title);
    }

    [Fact]
    public void Failure_reason_comes_from_error_messages()
    {
        var updated = PaymentWebhookHandler.Apply(Payment(PaymentStatus.AwaitingCustomer), Webhook(PushPaymentEvents.Failed));

        Assert.Equal("Insufficient funds", updated.StatusDetail);
    }
}
