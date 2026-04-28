using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Dim.Infrastructure.Persistence;
using Dim.Infrastructure.Redis;
using Ku.Utils.Database.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Dim.Infrastructure.DependencyInjection;

public static class DimInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDimInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var postgreSqlConnectionString = configuration["DimChat:Storage:PostgreSqlConnectionString"];
        if (!string.IsNullOrWhiteSpace(postgreSqlConnectionString))
        {
            services.AddDbContext<DimDbContext>(options => options.UseNpgsql(postgreSqlConnectionString));
        }

        var redisConnectionString = configuration["DimChat:Storage:RedisConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
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
