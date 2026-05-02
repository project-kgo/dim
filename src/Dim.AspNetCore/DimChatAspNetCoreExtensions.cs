using Dim.Abstractions.Authentication;
using Dim.Abstractions.Configuration;
using Dim.Abstractions.Runtime;
using Dim.Application.Runtime;
using Dim.Application.Routing;
using Dim.AspNetCore.Authentication;
using Dim.AspNetCore.Hubs;
using Dim.AspNetCore.Routing;
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
using Microsoft.Extensions.Options;

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
        services.TryAddSingleton<DimChatRouteService>();
        services.AddDimInfrastructure();
        services.TryAddSingleton<IUserIdProvider, DimAuthenticationUserIdProvider>();
        services.TryAddSingleton<IDimTokenValidator, DefaultDimTokenValidator>();
        services.AddSignalR();

        services
            .AddAuthentication(AuthConstants.Scheme)
            .AddScheme<AuthenticationSchemeOptions, DimTokenAuthenticationHandler>(
                AuthConstants.Scheme,
                _ => { });

        services.AddAuthorization();
        services.AddHostedService<OnlineTTLRefreshService>();

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
