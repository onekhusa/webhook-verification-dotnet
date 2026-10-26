using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneKhusa.PushPayments.Web.Shared.Payments;
using OneKhusa.Webhooks;
using OneKhusa.Webhooks.Verification;

namespace OneKhusa.PushPayments.Web.Tests.Features.ReceivePushPaymentWebhook;

public class PushPaymentWebhookEndpointTests : IDisposable
{
    private const string Url = "/webhooks/onekhusa/pushpayment";
    private const string SuccessSecret = "test-success-secret";
    private const string FailedSecret = "test-failed-secret";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PushPaymentWebhookEndpointTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // "Testing" keeps developer user-secrets out of the tests.
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OneKhusa:MerchantAccountNumber"] = "54833324",
                ["OneKhusa:Webhooks:Secrets:pushpayment.success"] = SuccessSecret,
                ["OneKhusa:Webhooks:Secrets:pushpayment.failed"] = FailedSecret,
                ["OneKhusa:Webhooks:Secrets:pushpayment.reversed"] = "",
            }));
        });
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private IPaymentStore Store => _factory.Services.GetRequiredService<IPaymentStore>();

    private PaymentRecord SeedPayment(string reference, PaymentStatus status = PaymentStatus.AwaitingCustomer)
    {
        var payment = new PaymentRecord
        {
            SourceReferenceNumber = reference,
            ConnectorId = 550044,
            PayerAccountNumber = "0999000000",
            Description = "Order #1",
            Amount = 15000m,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        Store.Add(payment);
        return payment;
    }

    private static string SuccessBody(string reference) => $$"""
        {"connectorId":550044,"sourceAccountNumber":"265880000000","sourceAccountName":"John Doe","sourceInstitution":"TNM MPAMBA",
         "sourceReferenceNumber":"{{reference}}","merchantReferenceNumber":"RAE-ABK-44728","beneficiaryAccountNumber":"54833324",
         "transactionReferenceNumber":"260921VK4NEG9V","transactionDescription":"Order #1","transactionAmount":15000,
         "transactionFee":157.5,"transactionDate":"2026-09-21T11:18:42.8474205+02:00","transactionStatusCode":"S",
         "transactionCode":"DMP","responseCode":"S100"}
        """;

    private static string FailedBody(string merchantReference) => $$"""
        {"connectorId":550044,"sourceAccountNumber":"265995558588","sourceInstitution":"Airtel Money",
         "merchantReferenceNumber":"{{merchantReference}}","beneficiaryAccountNumber":"54833324","transactionDescription":"Order #1",
         "transactionCode":"DMP","transactionAmount":100,"transactionDate":"2026-09-24T07:48:28.9498753Z",
         "transactionStatusCode":"F","responseCode":"E307","errorMessages":["Transaction Not Found"]}
        """;

    private Task<HttpResponseMessage> PostAsync(string eventName, string body, string? secret, string eventHeader = WebhookHeaders.Event)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(eventHeader, eventName);
        if (secret is not null)
            request.Headers.Add(WebhookHeaders.Signature, HmacSha512SignatureVerifier.ComputeSignature(secret, Encoding.UTF8.GetBytes(body)));
        return _client.SendAsync(request);
    }

    [Fact]
    public async Task Signed_success_marks_the_payment_paid()
    {
        SeedPayment("PPTEST000001");

        var response = await PostAsync(PushPaymentEvents.Success, SuccessBody("PPTEST000001"), SuccessSecret);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payment = Store.Find("PPTEST000001")!;
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal("260921VK4NEG9V", payment.TransactionReferenceNumber);
    }

    [Fact]
    public async Task Failed_webhook_is_matched_by_merchant_reference()
    {
        SeedPayment("PPTEST000002");

        var response = await PostAsync(PushPaymentEvents.Failed, FailedBody("PPTEST000002"), FailedSecret);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payment = Store.Find("PPTEST000002")!;
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Transaction Not Found", payment.StatusDetail);
    }

    [Fact]
    public async Task Invalid_signature_is_rejected_and_nothing_changes()
    {
        SeedPayment("PPTEST000003");

        var response = await PostAsync(PushPaymentEvents.Success, SuccessBody("PPTEST000003"), "wrong-secret");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(PaymentStatus.AwaitingCustomer, Store.Find("PPTEST000003")!.Status);
    }

    [Fact]
    public async Task Missing_signature_is_rejected()
    {
        SeedPayment("PPTEST000004");

        var response = await PostAsync(PushPaymentEvents.Success, SuccessBody("PPTEST000004"), secret: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(PaymentStatus.AwaitingCustomer, Store.Find("PPTEST000004")!.Status);
    }

    [Fact]
    public async Task Signature_from_another_events_secret_is_rejected()
    {
        SeedPayment("PPTEST000005");

        // A genuine "failed" signature replayed with the event header changed to "success".
        var response = await PostAsync(PushPaymentEvents.Success, SuccessBody("PPTEST000005"), FailedSecret);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(PaymentStatus.AwaitingCustomer, Store.Find("PPTEST000005")!.Status);
    }

    [Fact]
    public async Task Event_without_a_configured_secret_is_rejected()
    {
        SeedPayment("PPTEST000006", PaymentStatus.Paid);

        var response = await PostAsync(PushPaymentEvents.Reversed, SuccessBody("PPTEST000006"), "anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(PaymentStatus.Paid, Store.Find("PPTEST000006")!.Status);
    }

    [Fact]
    public async Task Alternate_event_header_spelling_is_accepted()
    {
        SeedPayment("PPTEST000007");

        var response = await PostAsync(PushPaymentEvents.Success, SuccessBody("PPTEST000007"), SuccessSecret, WebhookHeaders.EventAlternate);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PaymentStatus.Paid, Store.Find("PPTEST000007")!.Status);
    }

    [Fact]
    public async Task Redelivery_is_acknowledged_but_processed_once()
    {
        SeedPayment("PPTEST000008");
        var body = SuccessBody("PPTEST000008");

        var first = await PostAsync(PushPaymentEvents.Success, body, SuccessSecret);
        var second = await PostAsync(PushPaymentEvents.Success, body, SuccessSecret);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Contains("Duplicate", await second.Content.ReadAsStringAsync());
        Assert.Single(Store.Find("PPTEST000008")!.History, e => e.Title == "Payment received");
    }

    [Fact]
    public async Task Unknown_payment_is_acknowledged()
    {
        var response = await PostAsync(PushPaymentEvents.Success, SuccessBody("PPNOTKNOWN01"), SuccessSecret);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Signed_but_malformed_body_is_a_bad_request()
    {
        var response = await PostAsync(PushPaymentEvents.Success, "{ not json", SuccessSecret);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Oversized_body_is_rejected_before_verification()
    {
        var body = new string('x', 65 * 1024);

        var response = await PostAsync(PushPaymentEvents.Success, body, SuccessSecret);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }
}
