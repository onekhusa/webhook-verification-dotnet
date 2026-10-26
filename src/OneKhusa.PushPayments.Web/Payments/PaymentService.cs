using Microsoft.Extensions.Options;
using OneKhusa.Client.Configuration;
using OneKhusa.Client.PushPayments;

namespace OneKhusa.PushPayments.Web.Payments;

/// <summary>Starts a push payment: records it locally, then asks OneKhusa to prompt the customer.</summary>
public sealed class PaymentService(
    IPushPaymentClient pushPaymentClient,
    IPaymentStore store,
    IOptions<OneKhusaOptions> oneKhusaOptions,
    TimeProvider timeProvider)
{
    public async Task<PaymentRecord> StartAsync(PushPaymentForm form, string? connectorName, CancellationToken cancellationToken = default)
    {
        var reference = string.IsNullOrWhiteSpace(form.SourceReferenceNumber)
            ? SourceReference.New(timeProvider)
            : form.SourceReferenceNumber.Trim();
        if (store.Find(reference) is not null)
            throw new InvalidOperationException($"Source reference {reference} has already been used. Leave it empty to generate a new one.");

        var payerAccountNumber = form.PayerAccountNumber.Trim().TrimStart('+');
        var now = timeProvider.GetUtcNow();

        // Save before calling OneKhusa so a webhook can never arrive for a payment we don't know about.
        store.Add(new PaymentRecord
        {
            SourceReferenceNumber = reference,
            ConnectorId = form.ConnectorId,
            ConnectorName = connectorName,
            PayerAccountNumber = payerAccountNumber,
            Description = form.Description.Trim(),
            Amount = form.Amount,
            CreatedAt = now,
            UpdatedAt = now,
            History = [new PaymentEvent(now, "Push payment requested")],
        });

        var result = await pushPaymentClient.InitiateAsync(new PushPaymentRequest
        {
            MerchantAccountNumber = oneKhusaOptions.Value.MerchantAccountNumber,
            ConnectorId = form.ConnectorId,
            PayerAccountNumber = payerAccountNumber,
            SourceReferenceNumber = reference,
            TransactionDescription = form.Description.Trim(),
            TransactionAmount = form.Amount,
            CapturedBy = form.CapturedBy.Trim(),
        }, cancellationToken: cancellationToken);

        var at = timeProvider.GetUtcNow();
        return store.Update(reference, payment =>
        {
            // A webhook may already have moved the payment on; never overwrite that.
            if (payment.Status != PaymentStatus.Initiating)
                return payment;

            if (!result.IsSuccess)
            {
                return payment.WithEvent(new PaymentEvent(at, "OneKhusa rejected the request", result.Error.Message)) with
                {
                    Status = PaymentStatus.InitiationFailed,
                    StatusDetail = result.Error.Message,
                };
            }

            var response = result.Value;
            if (!response.IsInitiated)
            {
                var detail = $"Unexpected response code {response.ResponseCode}.";
                return payment.WithEvent(new PaymentEvent(at, "Prompt not sent", detail)) with
                {
                    Status = PaymentStatus.InitiationFailed,
                    StatusDetail = detail,
                };
            }

            return payment.WithEvent(new PaymentEvent(at, "Prompt sent to customer",
                $"Response {response.ResponseCode}. Customer pays {response.AmountToPay:N2} (charges {response.TotalCharges:N2}, fee rule {response.FeeRuleCode})."))
                with
                {
                    Status = PaymentStatus.AwaitingCustomer,
                    TotalCharges = response.TotalCharges,
                    AmountToPay = response.AmountToPay,
                    FeeRuleCode = response.FeeRuleCode,
                };
        })!;
    }
}
