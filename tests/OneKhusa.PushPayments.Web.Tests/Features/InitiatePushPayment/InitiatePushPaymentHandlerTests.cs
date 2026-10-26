using Microsoft.Extensions.Options;
using OneKhusa.Client.Common;
using OneKhusa.Client.Configuration;
using OneKhusa.Client.PushPayments;
using OneKhusa.PushPayments.Web.Features.InitiatePushPayment;
using OneKhusa.PushPayments.Web.Shared.Payments;

namespace OneKhusa.PushPayments.Web.Tests.Features.InitiatePushPayment;

public class InitiatePushPaymentHandlerTests
{
    private readonly InMemoryPaymentStore _store = new();
    private readonly FakePushPaymentClient _client = new();

    private InitiatePushPaymentHandler CreateHandler() => new(
        _client,
        _store,
        Options.Create(new OneKhusaOptions { MerchantAccountNumber = 71512541 }),
        TimeProvider.System);

    private static PushPaymentForm Form(string? reference = null) => new()
    {
        ConnectorId = 769375,
        PayerAccountNumber = " 221100000101 ",
        Amount = 500m,
        Description = "Order #1",
        CapturedBy = "your-email@email.com",
        SourceReferenceNumber = reference,
    };

    [Fact]
    public async Task Sends_the_request_and_marks_the_payment_awaiting_the_customer()
    {
        _client.Respond = request => OneKhusaResult<PushPaymentResponse>.Success(new PushPaymentResponse
        {
            SourceReferenceNumber = request.SourceReferenceNumber,
            ResponseCode = PushPaymentResponseCodes.Initiated,
            TotalCharges = 5m,
            AmountToPay = 505m,
            FeeRuleCode = FeeRuleCodes.ChargeCustomer,
        });

        var payment = await CreateHandler().HandleAsync(new InitiatePushPaymentCommand(Form(), "Airtel Money"));

        var sent = Assert.Single(_client.Requests);
        Assert.Equal(71512541, sent.MerchantAccountNumber);
        Assert.Equal("221100000101", sent.PayerAccountNumber);
        Assert.Equal(payment.SourceReferenceNumber, sent.SourceReferenceNumber);
        Assert.InRange(sent.SourceReferenceNumber.Length, 5, 25);

        Assert.Equal(PaymentStatus.AwaitingCustomer, payment.Status);
        Assert.Equal(505m, payment.AmountToPay);
        Assert.Same(payment, _store.Find(payment.SourceReferenceNumber));
    }

    [Fact]
    public async Task Payment_is_saved_before_OneKhusa_is_called()
    {
        PaymentRecord? savedBeforeCall = null;
        _client.Respond = request =>
        {
            savedBeforeCall = _store.Find(request.SourceReferenceNumber);
            return OneKhusaResult<PushPaymentResponse>.Success(new PushPaymentResponse { ResponseCode = PushPaymentResponseCodes.Initiated });
        };

        await CreateHandler().HandleAsync(new InitiatePushPaymentCommand(Form(), null));

        Assert.NotNull(savedBeforeCall);
        Assert.Equal(PaymentStatus.Initiating, savedBeforeCall.Status);
    }

    [Fact]
    public async Task Api_error_marks_the_payment_not_initiated()
    {
        _client.Respond = _ => OneKhusaResult<PushPaymentResponse>.Failure(
            new OneKhusaError("OneKhusa returned HTTP 400: 0999000000 is not a correct test phone number (E900)"));

        var payment = await CreateHandler().HandleAsync(new InitiatePushPaymentCommand(Form(), null));

        Assert.Equal(PaymentStatus.InitiationFailed, payment.Status);
        Assert.Contains("E900", payment.StatusDetail);
    }

    [Fact]
    public async Task A_webhook_that_arrives_first_is_not_overwritten()
    {
        // The customer approves so fast that the success webhook lands before the initiate response.
        _client.Respond = request =>
        {
            _store.Update(request.SourceReferenceNumber, p => p with { Status = PaymentStatus.Paid });
            return OneKhusaResult<PushPaymentResponse>.Success(new PushPaymentResponse { ResponseCode = PushPaymentResponseCodes.Initiated });
        };

        var payment = await CreateHandler().HandleAsync(new InitiatePushPaymentCommand(Form(), null));

        Assert.Equal(PaymentStatus.Paid, payment.Status);
    }

    [Fact]
    public async Task Reusing_a_source_reference_is_rejected()
    {
        _client.Respond = _ => OneKhusaResult<PushPaymentResponse>.Success(new PushPaymentResponse { ResponseCode = PushPaymentResponseCodes.Initiated });
        var handler = CreateHandler();
        await handler.HandleAsync(new InitiatePushPaymentCommand(Form("ORDER-1001"), null));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(new InitiatePushPaymentCommand(Form("ORDER-1001"), null)));
        Assert.Single(_client.Requests);
    }

    private sealed class FakePushPaymentClient : IPushPaymentClient
    {
        public List<PushPaymentRequest> Requests { get; } = [];

        public Func<PushPaymentRequest, OneKhusaResult<PushPaymentResponse>> Respond { get; set; } =
            _ => throw new InvalidOperationException("No response configured.");

        public Task<OneKhusaResult<PushPaymentResponse>> InitiateAsync(
            PushPaymentRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(Respond(request));
        }
    }
}
