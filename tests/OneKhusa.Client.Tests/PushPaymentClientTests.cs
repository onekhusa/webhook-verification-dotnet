using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OneKhusa.Client.Connectors;
using OneKhusa.Client.PushPayments;

namespace OneKhusa.Client.Tests;

public class PushPaymentClientTests
{
    private const string TokenPath = "/account/getAccessToken";
    private const string InitiatePath = "/collections/push/direct/initiate";

    private readonly FakeOneKhusaHandler _handler = new();
    private readonly ServiceProvider _services;

    public PushPaymentClientTests()
    {
        var time = new ManualTimeProvider(TestServices.Now);
        _handler.Respond(TokenPath, HttpStatusCode.OK, TestServices.TokenJson(TestServices.Now.AddMinutes(5)));
        _services = TestServices.Build(_handler, time);
    }

    private static PushPaymentRequest ValidRequest() => new()
    {
        MerchantAccountNumber = 12345678,
        ConnectorId = 550044,
        PayerAccountNumber = "0999000000",
        SourceReferenceNumber = "OKPG90WOIJD98JKSJKH",
        TransactionDescription = "Groceries Order Payment",
        TransactionAmount = 100m,
        CapturedBy = "john.doe@onekhusa.com",
    };

    [Fact]
    public async Task Initiate_sends_the_documented_request()
    {
        _handler.Respond(InitiatePath, HttpStatusCode.OK, """
            {"merchantAccountNumber":12345678,"sourceReferenceNumber":"OKPG90WOIJD98JKSJKH","responseCode":"I100",
             "feeRuleCode":"CC","transactionAmount":100,"totalCharges":2.5,"amountToPay":102.5}
            """);
        var client = _services.GetRequiredService<IPushPaymentClient>();

        var result = await client.InitiateAsync(ValidRequest(), idempotencyKey: "key-1");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsInitiated);
        Assert.Equal(102.5m, result.Value.AmountToPay);
        Assert.Equal(FeeRuleCodes.ChargeCustomer, result.Value.FeeRuleCode);

        var request = _handler.Requests.Single(r => r.Uri.AbsolutePath.EndsWith(InitiatePath, StringComparison.Ordinal));
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.onekhusa.com/sandbox/v1/collections/push/direct/initiate", request.Uri.ToString());
        Assert.Equal("Bearer jwt-token", request.Headers["Authorization"]);
        Assert.Equal("key-1", request.Headers["X-Idempotency-Key"]);
        Assert.Equal("en", request.Headers["Accept-Language"]);

        using var body = JsonDocument.Parse(request.Body!);
        var root = body.RootElement;
        Assert.Equal(12345678, root.GetProperty("merchantAccountNumber").GetInt32());
        Assert.Equal(550044, root.GetProperty("connectorId").GetInt32());
        Assert.Equal("0999000000", root.GetProperty("payerAccountNumber").GetString());
        Assert.Equal("OKPG90WOIJD98JKSJKH", root.GetProperty("sourceReferenceNumber").GetString());
        Assert.Equal("Groceries Order Payment", root.GetProperty("transactionDescription").GetString());
        Assert.Equal(100m, root.GetProperty("transactionAmount").GetDecimal());
        Assert.Equal("john.doe@onekhusa.com", root.GetProperty("capturedBy").GetString());
    }

    [Fact]
    public async Task Generates_an_idempotency_key_when_none_is_given()
    {
        _handler.Respond(InitiatePath, HttpStatusCode.OK, """{"responseCode":"I100"}""");
        var client = _services.GetRequiredService<IPushPaymentClient>();

        await client.InitiateAsync(ValidRequest());

        var request = _handler.Requests.Single(r => r.Uri.AbsolutePath.EndsWith(InitiatePath, StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(request.Headers["X-Idempotency-Key"]));
    }

    [Fact]
    public async Task Api_error_is_returned_with_status_and_body()
    {
        _handler.Respond(InitiatePath, HttpStatusCode.BadRequest, """{"errorMessages":["Duplicate source reference number"]}""");
        var client = _services.GetRequiredService<IPushPaymentClient>();

        var result = await client.InitiateAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, result.Error.StatusCode);
        Assert.Contains("Duplicate source reference number", result.Error.Message);
        Assert.Contains("errorMessages", result.Error.ResponseBody);
    }

    [Fact]
    public async Task Invalid_request_is_rejected_before_calling_the_api()
    {
        var client = _services.GetRequiredService<IPushPaymentClient>();

        var result = await client.InitiateAsync(ValidRequest() with { SourceReferenceNumber = "ABC", TransactionAmount = 0 });

        Assert.False(result.IsSuccess);
        Assert.Contains("SourceReferenceNumber", result.Error.Message);
        Assert.Contains("TransactionAmount", result.Error.Message);
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task Token_failure_is_returned_as_an_error()
    {
        _handler.Respond(TokenPath, HttpStatusCode.Unauthorized, "{}");
        var client = _services.GetRequiredService<IPushPaymentClient>();

        var result = await client.InitiateAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Unauthorized, result.Error.StatusCode);
        Assert.Equal(0, _handler.CountRequests(InitiatePath));
    }

    [Fact]
    public async Task Connectors_are_read_from_GetAll()
    {
        _handler.Respond("/core/connectors/GetAll", HttpStatusCode.OK, """
            [{"connectorId":769375,"connectorName":"Airtel Money","connectorCode":"EEE","channelName":"Mobile Network Operator",
              "channelCode":"M","switchMerchantId":"5554","logoBase64":"iVBOR","logoFormat":"image/png","isActive":true},
             {"connectorId":769356,"connectorName":"FDH Bank Limited","connectorCode":"FDH","channelName":"Bank",
              "channelCode":"B","logoBase64":"iVBOR","logoFormat":"PNG","isActive":true}]
            """);
        var client = _services.GetRequiredService<IConnectorsClient>();

        var result = await client.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Collection(result.Value,
            airtel =>
            {
                Assert.True(airtel.IsMobileMoney);
                Assert.Equal("data:image/png;base64,iVBOR", airtel.LogoDataUri);
            },
            fdh =>
            {
                Assert.False(fdh.IsMobileMoney);
                Assert.Equal("data:image/png;base64,iVBOR", fdh.LogoDataUri);
            });
    }
}
