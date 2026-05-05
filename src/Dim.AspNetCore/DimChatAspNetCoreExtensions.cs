using Dim.Abstractions.Authentication;
using Dim.Abstractions.Configuration;
using Dim.Abstractions.Runtime;
using Dim.Abstractions.Signaling;
using Dim.Application.Authentication;
using Dim.Application.Runtime;
using Dim.Application.Routing;
using Dim.Application.Signaling;
using Dim.AspNetCore.Authentication;
using Dim.AspNetCore.Hubs;
using Dim.AspNetCore.Routing;
using Dim.AspNetCore.Signaling;
using Dim.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ku.Utils.Snowflake;

namespace Dim.AspNetCore;

public static class DimChatAspNetCoreExtensions
{
    public static IServiceCollection AddDimChat(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<DimChatOptions>()
            .Bind(configuration.GetSection(DimChatOptions.SectionName))
            .ValidateDataAnnotations();
        services.TryAddSingleton<IDimChatRuntime, DimChatRuntime>();
        services.TryAddSingleton<DimServerIdentity>();
        services.TryAddSingleton<DimChatRouteService>();
        services.TryAddSingleton<IDimSignalSender, DimSignalSender>();
        services.AddDimInfrastructure(configuration);
        services.TryAddSingleton<IUserIdProvider, DimAuthenticationUserIdProvider>();
        services.TryAddSingleton<DImAuthentication>();
        services.TryAddSingleton<IAuthentication>(serviceProvider =>
            serviceProvider.GetRequiredService<DImAuthentication>());
        services.TryAddSingleton<ITokenValidator>(serviceProvider =>
            serviceProvider.GetRequiredService<DImAuthentication>());
        services.TryAddSingleton<IDimLocalSignalDispatcher, DimSignalHubDispatcher>();
        services.AddSignalR();

        services.TryAddSingleton<DistributedSnowflake>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DimChatOptions>>()
                .Value;
            var connectionString = options.Storage.PgMasterSqlConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("DimChat:Storage:PgMasterSqlConnectionString 未配置。");
            }

            return DistributedSnowflake.CreateAsync(
                new DistributedSnowflakeOptions
                {
                    ConnectionString = connectionString,
                },
                logger: sp.GetService<ILogger<DistributedSnowflake>>()
            ).ConfigureAwait(false).GetAwaiter().GetResult();
        });

        services
            .AddAuthentication(AuthConstants.Scheme)
            .AddScheme<AuthenticationSchemeOptions, DimTokenAuthenticationHandler>(
                AuthConstants.Scheme,
                _ => { });

        services.AddAuthorization();
        services.AddHostedService<OnlineTTLRefreshService>();
        services.AddHostedService<DimSignalSubscriptionService>();

        return services;
    }

    public static IEndpointRouteBuilder MapDimChat(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<DimChatOptions>>()
            .Value;

        var endpointPrefix = NormalizePrefix(options.EndpointPrefix);
        var hubPath = NormalizeSegment(options.HubPath);

        var group = endpoints.MapGroup(endpointPrefix);

        group.MapGet("/health", (IDimChatRuntime runtime) =>
        {
            var status = runtime.GetStatus();
            return Results.Ok(new DimChatHealthResponse(
                status.Name,
                status.Version,
                status.State));
        });

        endpoints
            .MapHub<DimChatHub>(
                $"{endpointPrefix}/{hubPath}",
                options => options.Transports = HttpTransportType.WebSockets)
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = AuthConstants.Scheme
            });

        return endpoints;
    }

    private static string NormalizePrefix(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "/dim" : value.Trim();
        return normalized.StartsWith('/') ? normalized.TrimEnd('/') : $"/{normalized.TrimEnd('/')}";
    }

    private static string NormalizeSegment(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "hub" : value.Trim();
        return normalized.Trim('/');
    }
}
