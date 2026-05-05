using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Dim.Application.Authentication;
using Dim.Application.Signaling;
using Dim.Infrastructure.Authentication;
using Dim.Infrastructure.Persistence;
using Dim.Infrastructure.Routing;
using Dim.Infrastructure.Signaling;
using DotNetCore.CAP;
using Ku.Utils.Database.Redis;
using Ku.Utils.Database.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using DotNetCore.CAP.Internal;
using Dim.Infrastructure.Snowflake;

namespace Dim.Infrastructure.DependencyInjection;

public static class DimInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDimInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration
            .GetSection(DimChatOptions.SectionName)
            .Get<DimChatOptions>() ?? new DimChatOptions();

        services.AddDimCap(options.Storage);
        return services.AddDimInfrastructure();
    }

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

        services.AddDbContext<DimMasterDbContext>((sp, options) =>
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

        if (!services.Any(static descriptor => descriptor.ServiceType == typeof(IDimChatRouteStore)))
        {
            services.TryAddSingleton<DimRouteStore>();
            services.TryAddSingleton<IDimChatRouteStore>(serviceProvider =>
                serviceProvider.GetRequiredService<DimRouteStore>());
        }

        services.AddHostedService<DimRouteStoreScriptPreloadService>();
        services.TryAddSingleton<ITokenValidatorStore, RedisTokenValidatorStore>();

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
                serviceProvider.GetRequiredService<Dim.Application.Routing.DimServerIdentity>(),
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
        services.AddDimCap(dimChatOptions.Value.Storage);
        return services.AddDimInfrastructure();
    }

    private static IServiceCollection AddDimCap(
        this IServiceCollection services,
        DimChatStorageOptions storageOptions)
    {
        if (services.Any(static descriptor => descriptor.ServiceType == typeof(ICapPublisher)))
        {
            return services;
        }

        if (string.IsNullOrWhiteSpace(storageOptions.PgMasterSqlConnectionString) ||
            string.IsNullOrWhiteSpace(storageOptions.RedisConnectionString))
        {
            return services;
        }

        var connectionString = storageOptions.PgMasterSqlConnectionString;
        var redisConnectionString = storageOptions.RedisConnectionString;
        var streamNamePrefix = NormalizeOptional(storageOptions.RedisStreamName);
        var capSchema = NormalizeOptional(storageOptions.CapStorageSchema) ?? "cap";
        var defaultGroupName = NormalizeOptional(storageOptions.CapDefaultGroupName) ?? "dim";

        services.AddCap(options =>
        {
            options.DefaultGroupName = defaultGroupName;
            options.UseStorageLock = true;
            options.EnablePublishParallelSend = true;

            if (streamNamePrefix is not null)
            {
                options.TopicNamePrefix = streamNamePrefix;
            }

            options.UsePostgreSql(postgreSqlOptions =>
            {
                postgreSqlOptions.ConnectionString = connectionString;
                // postgreSqlOptions.DataSource = sp.GetRequiredService<DimMasterDataSource>().Value;
                postgreSqlOptions.Schema = capSchema;
            });

            options.UseRedis(redisConnectionString);
        });

        services.AddSingleton<ISnowflakeId, SnowflakeCap>();

        return services;
    }

    private static string GetRequiredConnectionString(string? connectionString, string configurationKey)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException($"{configurationKey} 未配置。");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
