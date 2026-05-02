using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Dim.Application.Signaling;
using Dim.Infrastructure.Persistence;
using Dim.Infrastructure.Routing;
using Dim.Infrastructure.Signaling;
using Ku.Utils.Database.Redis;
using Ku.Utils.Database.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using StackExchange.Redis;

namespace Dim.Infrastructure.DependencyInjection;

public static class DimInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDimInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<DimChatOptions>>()
                .Value;

            var masterConnectionString = GetRequiredConnectionString(
                options.Storage.PgMasterSqlConnectionString,
                "DimChat:Storage:PgMasterSqlConnectionString");

            var dataSource = PostgreSqlDataSourceFactory.GetOrCreate(new PostgreSqlConnectionOptions
            {
                ConnectionString = masterConnectionString
            });

            return new DimMasterDataSource(dataSource);
        });

        services.TryAddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<DimChatOptions>>()
                .Value;

            var masterConnectionString = GetRequiredConnectionString(
                options.Storage.PgMasterSqlConnectionString,
                "DimChat:Storage:PgMasterSqlConnectionString");

            var slaveConnectionString = string.IsNullOrWhiteSpace(options.Storage.PgSlaveSqlConnectionString)
                ? masterConnectionString
                : options.Storage.PgSlaveSqlConnectionString;

            var dataSource = PostgreSqlDataSourceFactory.GetOrCreate(new PostgreSqlConnectionOptions
            {
                ConnectionString = slaveConnectionString
            });

            return new DimSlaveDataSource(dataSource);
        });

        services.AddDbContext<DimDbContext>((sp, options) =>
        {
            var dataSource = sp.GetRequiredService<DimMasterDataSource>().Value;
            options.UseNpgsql(dataSource);
        });

        services.AddDbContext<DimSlaveDbContext>((sp, options) =>
        {
            var dataSource = sp.GetRequiredService<DimSlaveDataSource>().Value;
            options.UseNpgsql(dataSource);
        });

        services.TryAddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<DimChatOptions>>()
                .Value;

            var redisConnectionString = GetRequiredConnectionString(
                options.Storage.RedisConnectionString,
                "DimChat:Storage:RedisConnectionString");

            return RedisConnectionFactory.GetOrCreate(new RedisConnectionOptions
            {
                ConnectionString = redisConnectionString
            });
        });

        services.TryAddSingleton<IDimChatRouteStore>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<DimChatOptions>>();

            if (string.IsNullOrWhiteSpace(options.Value.Storage.RedisConnectionString))
            {
                return new MissingDimRouteStore();
            }

            return new DimRouteStore(
                serviceProvider.GetRequiredService<IConnectionMultiplexer>(),
                options);
        });

        services.TryAddSingleton<IDimSignalBus>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<DimChatOptions>>();

            if (string.IsNullOrWhiteSpace(options.Value.Storage.RedisConnectionString))
            {
                return new MissingDimSignalBus();
            }

            return new RedisDimSignalBus(
                serviceProvider.GetRequiredService<IConnectionMultiplexer>(),
                options,
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisDimSignalBus>>());
        });

        services.AddSingleton<ILocalConnectionRouteStore, LocalConnectionRouteStore>();

        return services;
    }

    public static IServiceCollection AddDimInfrastructure(
        this IServiceCollection services,
        IOptions<DimChatOptions> dimChatOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dimChatOptions);

        services.TryAddSingleton(dimChatOptions);
        return services.AddDimInfrastructure();
    }

    private static string GetRequiredConnectionString(string? connectionString, string configurationKey)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException($"{configurationKey} 未配置。");
    }
}
