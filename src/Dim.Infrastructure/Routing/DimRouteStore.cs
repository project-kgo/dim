using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Dim.Infrastructure.Routing;

public sealed class DimRouteStore(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<DimChatOptions> options) : IDimChatRouteStore
{
    private const string connectMultiPlatformScript = """
        local current = redis.call("HGET", KEYS[1], ARGV[1])
        redis.call("HSETEX", KEYS[1], "EX", ARGV[3], "FIELDS", 1, ARGV[1], ARGV[2])
        if current then
            return { current }
        end
        return {}
        """;
    private const string connectSinglePlatformScript = """
        local hlen = redis.call("HLEN", KEYS[1])
        if hlen == 0 then
            redis.call("HSETEX", KEYS[1], "EX", ARGV[3], "FIELDS", 1, ARGV[1], ARGV[2])
            return {}
        end
        local values = redis.call("HVALS", KEYS[1])
        redis.call("DEL", KEYS[1])
        redis.call("HSETEX", KEYS[1], "EX", ARGV[3], "FIELDS", 1, ARGV[1], ARGV[2])
        return values
        """;
    private const string disconnectScript = """
        local connectionId = redis.call("HGET", KEYS[1], ARGV[1])
        if not connectionId or connectionId ~= ARGV[2] then
            return 0
        end
        redis.call("HDEL", KEYS[1], ARGV[1])
        return 1
        """;

    private const string refreshScript = """
        local current = redis.call("HGET", KEYS[1], ARGV[1])
        if current and current == ARGV[2] then
            redis.call("HEXPIRE", KEYS[1], ARGV[3], "FIELDS", 1, ARGV[1])
            return 1
        end
        return 0
        """;

    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();
    private readonly bool _allowMultiDeviceLogin = options.Value.Connection.AllowMultiDeviceLogin;

    private readonly string _routeKeyPrefix = NormalizePrefix(options.Value.Connection.RouteKeyPrefix);

    public async ValueTask<string[]?> SetRouteAsync(
        DimChatRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        var script = _allowMultiDeviceLogin ? connectMultiPlatformScript : connectSinglePlatformScript;
        var result = await _database.ScriptEvaluateAsync(
            script,
            [RouteKey(route)],
            [
                route.Platform.ToString(),
                route.ConnectionId,
                checked((long)NormalizeTtl(ttl).TotalSeconds),
            ]
        );

        var list = (RedisResult[])result!;
        if (list is null or [])
        {
            return null;
        }

        return [.. list.Select(x => x.ToString())];
    }

    public async ValueTask<bool> RemoveRouteAsync(
        DimChatRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _database.ScriptEvaluateAsync(
            disconnectScript,
            [RouteKey(route)],
            [route.Platform.ToString(), route.ConnectionId]
        );
        return (int)result! > 0;
    }

    public async ValueTask<bool> RefreshRouteAsync(
        DimChatRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        var result = await _database.ScriptEvaluateAsync(
            refreshScript,
            [RouteKey(route)],
            [
                route.Platform.ToString(),
                route.ConnectionId,
                checked((long)NormalizeTtl(ttl).TotalSeconds),
            ]
        );
        return (int)result! > 0;
    }

    private string RouteKey(DimChatRoute route)
    {
        return $"{_routeKeyPrefix}:{{{route.UserId}}}";
    }

    private static string NormalizePrefix(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "dim:routes" : value.Trim();
        return normalized.Trim(':');
    }

    private static TimeSpan NormalizeTtl(TimeSpan ttl)
    {
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.FromDays(7);
    }
}
