using OneKhusa.Client.DependencyInjection;
using OneKhusa.PushPayments.Web.Components;
using OneKhusa.PushPayments.Web.Configuration;
using OneKhusa.PushPayments.Web.Diagnostics;
using OneKhusa.PushPayments.Web.Payments;
using OneKhusa.Webhooks.DependencyInjection;
using OneKhusa.Webhooks.Diagnostics;
using OneKhusa.Webhooks.Endpoints;
using OneKhusa.Webhooks.Handling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 1. OneKhusa API client: access tokens, connectors, push payment initiation.
builder.Services.AddOneKhusaClient(builder.Configuration);

// 2. OneKhusa webhook receiver: signature verification and deduplication,
//    plus the application's handler that decides what each event means for a payment.
builder.Services.AddOneKhusaWebhooks(builder.Configuration);
builder.Services.AddScoped<IPushPaymentWebhookHandler, PaymentWebhookHandler>();

// 3. Demo application: in-memory payments, webhook inspector and simulator.
builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection(DemoOptions.SectionName));
builder.Services.AddSingleton<IPaymentStore, InMemoryPaymentStore>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddSingleton<WebhookLog>();
builder.Services.AddSingleton<IWebhookDeliveryObserver>(services => services.GetRequiredService<WebhookLog>());
builder.Services.AddHttpClient(WebhookSimulator.HttpClientName);
builder.Services.AddScoped<WebhookSimulator>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Register this URL in the OneKhusa portal for the pushpayment.* events.
app.MapOneKhusaPushPaymentWebhook();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
