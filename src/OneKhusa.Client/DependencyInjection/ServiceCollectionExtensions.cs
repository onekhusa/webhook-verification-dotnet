using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OneKhusa.Client.Authentication;
using OneKhusa.Client.Configuration;
using OneKhusa.Client.Connectors;
using OneKhusa.Client.PushPayments;

namespace OneKhusa.Client.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAccessTokenProvider"/>, <see cref="IConnectorsClient"/> and
    /// <see cref="IPushPaymentClient"/>, configured from the <c>OneKhusa</c> section.
    /// </summary>
    public static IServiceCollection AddOneKhusaClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OneKhusaOptions>().Bind(configuration.GetSection(OneKhusaOptions.SectionName));

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IAccessTokenProvider, AccessTokenProvider>();
        services.AddTransient<BearerTokenHandler>();

        // The token endpoint is called without a bearer token; every other call goes through BearerTokenHandler.
        services.AddHttpClient(AccessTokenProvider.HttpClientName, ConfigureHttpClient);
        services.AddHttpClient<IConnectorsClient, ConnectorsClient>(ConfigureHttpClient)
            .AddHttpMessageHandler<BearerTokenHandler>();
        services.AddHttpClient<IPushPaymentClient, PushPaymentClient>(ConfigureHttpClient)
            .AddHttpMessageHandler<BearerTokenHandler>();

        return services;
    }

    private static void ConfigureHttpClient(IServiceProvider services, HttpClient client)
    {
        var options = services.GetRequiredService<IOptions<OneKhusaOptions>>().Value;

        // The trailing slash keeps "/v1" when relative paths such as "account/getAccessToken" are combined.
        if (Uri.TryCreate(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseAddress))
            client.BaseAddress = baseAddress;

        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        if (!string.IsNullOrWhiteSpace(options.Language))
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(options.Language);
    }
}
