using OneKhusa.Client.DependencyInjection;
using OneKhusa.PushPayments.Web.Components;
using OneKhusa.PushPayments.Web.Features.InitiatePushPayment;
using OneKhusa.PushPayments.Web.Features.ReceivePushPaymentWebhook;
using OneKhusa.PushPayments.Web.Features.WebhookEvents;
using OneKhusa.PushPayments.Web.Shared.Payments;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Shared: the OneKhusa API client (tokens, connectors, push initiation) and the payment store.
builder.Services.AddOneKhusaClient(builder.Configuration);
builder.Services.AddPaymentStore();

// Feature slices. Each one lives in its own folder under Features/.
builder.Services
    .AddInitiatePushPayment(builder.Configuration)          // Step 5: send the payment prompt
    .AddReceivePushPaymentWebhook(builder.Configuration)    // Steps 6-9: verify and apply webhooks
    .AddWebhookEvents();                                    // Step 10: inspect and simulate deliveries

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// POST /webhooks/onekhusa/pushpayment. Register this URL in the OneKhusa portal.
app.MapReceivePushPaymentWebhook();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
