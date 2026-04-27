using Dim.Infrastructure.Persistence;
using Dim.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            services.AddDbContext<DimDbContext>(options =>
            {
                options.UseNpgsql(postgreSqlConnectionString);
            });
        }

        services.TryAddSingleton<DimRedisStreamOptions>();

        return services;
    }
}
