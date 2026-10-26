using System.Text.Json;
using OneKhusa.Webhooks.Models;

namespace OneKhusa.Webhooks.Tests;

/// <summary>Payloads copied from the Push Payment Webhook docs page.</summary>
public class PushPaymentWebhookPayloadTests
{
    [Fact]
    public void Deserializes_success_payload()
    {
        const string json = """
            {
              "connectorId": 550044,
              "sourceAccountNumber": "265880000000",
              "sourceAccountName": "John Doe",
              "sourceInstitution": "TNM MPAMBA",
              "sourceReferenceNumber": "DII338KORK1",
              "merchantReferenceNumber": "RAE-ABK-44728",
              "beneficiaryAccountNumber": "54833324",
              "transactionReferenceNumber": "260921VK4NEG9V",
              "transactionDescription": "Payment for Order #10234",
              "transactionAmount": 15000,
              "transactionFee": 157.5,
              "transactionDate": "2026-09-21T11:18:42.8474205+02:00",
              "transactionStatusCode": "S",
              "transactionCode": "DMP",
              "responseCode": "S100"
            }
            """;

        var payload = JsonSerializer.Deserialize<PushPaymentWebhookPayload>(json)!;

        Assert.Equal(550044, payload.ConnectorId);
        Assert.Equal("DII338KORK1", payload.SourceReferenceNumber);
        Assert.Equal(15000m, payload.TransactionAmount);
        Assert.Equal(157.5m, payload.TransactionFee);
        Assert.Equal(TimeSpan.FromHours(2), payload.TransactionDate!.Value.Offset);
        Assert.Equal(TransactionStatus.Success, payload.Status);
        Assert.Null(payload.ErrorMessages);
    }

    [Fact]
    public void Deserializes_failed_payload()
    {
        const string json = """
            {
              "connectorId": 550044,
              "sourceAccountNumber": "265995558588",
              "sourceInstitution": "Airtel Money",
              "merchantReferenceNumber": "RAE-ABK-44728",
              "beneficiaryAccountNumber": "54833324",
              "transactionDescription": "Payment for Order #10233",
              "transactionCode": "DMP",
              "transactionAmount": 100,
              "transactionDate": "2026-09-24T07:48:28.9498753Z",
              "transactionStatusCode": "F",
              "responseCode": "E307",
              "errorMessages": [ "Transaction Not Found" ]
            }
            """;

        var payload = JsonSerializer.Deserialize<PushPaymentWebhookPayload>(json)!;

        Assert.Equal(TransactionStatus.Failed, payload.Status);
        Assert.Null(payload.SourceReferenceNumber);
        Assert.Null(payload.TransactionFee);
        Assert.Equal(["Transaction Not Found"], payload.ErrorMessages!);
    }

    [Theory]
    [InlineData("R", TransactionStatus.Reversed)]
    [InlineData("s", TransactionStatus.Success)]
    [InlineData("X", TransactionStatus.Unknown)]
    [InlineData(null, TransactionStatus.Unknown)]
    public void Maps_status_codes(string? code, TransactionStatus expected)
    {
        Assert.Equal(expected, new PushPaymentWebhookPayload { TransactionStatusCode = code }.Status);
    }
}
