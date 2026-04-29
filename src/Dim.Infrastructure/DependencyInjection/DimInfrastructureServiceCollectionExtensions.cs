using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Dim.Infrastructure.Persistence;
using Dim.Infrastructure.Redis;
using Ku.Utils.Database.Redis;
using Ku.Utils.Database.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using StackExchange.Redis;

namespace Dim.Infrastructure.DependencyInjection;

public static class DimInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDimInfrastructure(
        this IServiceCollection services,
        IOptions<DimChatOptions> dimChatOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dimChatOptions);

        var storageOptions = dimChatOptions.Value.Storage;
        ArgumentNullException.ThrowIfNull(storageOptions);

        var masterDataSource = PostgreSqlDataSourceFactory.GetOrCreate(new PostgreSqlConnectionOptions
        {
            ConnectionString = storageOptions.PgMasterSqlConnectionString!
        });

        var pgSlaveSqlConnectionString = storageOptions.PgSlaveSqlConnectionString;
        pgSlaveSqlConnectionString ??= storageOptions.PgMasterSqlConnectionString;

        var slaveDataSource = PostgreSqlDataSourceFactory.GetOrCreate(new PostgreSqlConnectionOptions
        {
            ConnectionString = pgSlaveSqlConnectionString!
        });

        services.TryAddSingleton(new DimMasterDataSource(masterDataSource));
        services.TryAddSingleton(new DimSlaveDataSource(slaveDataSource));


        services.AddDbContext<DimDbContext>((sp, options) => {
            var ds = sp.GetRequiredService<DimMasterDataSource>().Value;
            options.UseNpgsql(ds);
        });

        services.AddDbContext<DimSlaveDbContext>((sp, options) => {
            var ds = sp.GetRequiredService<DimSlaveDataSource>().Value;
            options.UseNpgsql(ds);
        });

        if (!string.IsNullOrWhiteSpace(storageOptions.RedisConnectionString))
        {
            services.TryAddSingleton<IConnectionMultiplexer>(serviceProvider =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<DimChatOptions>>()
                    .Value;

                return RedisConnectionFactory.GetOrCreate(new RedisConnectionOptions
                {
                    ConnectionString = options.Storage.RedisConnectionString!
                });
            });
            services.TryAddSingleton<IDimChatRouteStore, RedisDimChatRouteStore>();
        }
        else
        {
            services.TryAddSingleton<IDimChatRouteStore, MissingRedisDimChatRouteStore>();
        }

        services.TryAddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<DimChatOptions>>()
                .Value;

            return new DimRedisStreamOptions
            {
                StreamName = options.Storage.RedisStreamName
            };
        });

        return services;
    }
}
