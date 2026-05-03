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
        local connectionField = ARGV[1] .. "_c"
        local serverField = ARGV[1] .. "_s"
        local currentConnectionId = redis.call("HGET", KEYS[1], connectionField)
        local currentServerId = redis.call("HGET", KEYS[1], serverField)
        redis.call("HSETEX", KEYS[1], "EX", ARGV[3], "FIELDS", 2, connectionField, ARGV[2], serverField, ARGV[4])
        if currentConnectionId and currentServerId then
            return { ARGV[1], currentConnectionId, currentServerId }
        end
        return {}
        """;
    private const string connectSinglePlatformScript = """
        local hlen = redis.call("HLEN", KEYS[1])
        local connectionField = ARGV[1] .. "_c"
        local serverField = ARGV[1] .. "_s"
        if hlen == 0 then
            redis.call("HSETEX", KEYS[1], "EX", ARGV[3], "FIELDS", 2, connectionField, ARGV[2], serverField, ARGV[4])
            return {}
        end
        local entries = redis.call("HGETALL", KEYS[1])
        local values = {}
        for i = 1, #entries, 2 do
            local field = entries[i]
            if string.sub(field, -2) == "_c" then
                local platform = string.sub(field, 1, -3)
                local serverId = redis.call("HGET", KEYS[1], platform .. "_s")
                if serverId then
                    table.insert(values, platform)
                    table.insert(values, entries[i + 1])
                    table.insert(values, serverId)
                end
            end
        end
        redis.call("DEL", KEYS[1])
        redis.call("HSETEX", KEYS[1], "EX", ARGV[3], "FIELDS", 2, connectionField, ARGV[2], serverField, ARGV[4])
        return values
        """;
    private const string disconnectScript = """
        local connectionField = ARGV[1] .. "_c"
        local serverField = ARGV[1] .. "_s"
        local connectionId = redis.call("HGET", KEYS[1], connectionField)
        if not connectionId or connectionId ~= ARGV[2] then
            return 0
        end
        redis.call("HDEL", KEYS[1], connectionField)
        redis.call("HDEL", KEYS[1], serverField)
        return 1
        """;

    private const string refreshScript = """
        local connectionField = ARGV[1] .. "_c"
        local serverField = ARGV[1] .. "_s"
        local current = redis.call("HGET", KEYS[1], connectionField)
        if current and current == ARGV[2] then
            redis.call("HEXPIRE", KEYS[1], ARGV[3], "FIELDS", 2, connectionField, serverField)
            return 1
        end
        return 0
        """;

    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();
    private readonly bool _allowMultiDeviceLogin = options.Value.Connection.AllowMultiDeviceLogin;

    private readonly string _routeKeyPrefix = NormalizePrefix(options.Value.Connection.RouteKeyPrefix);

    public async ValueTask<DimReplacedConnectionRoute[]?> SetRouteAsync(
        DimConectionRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        var script = _allowMultiDeviceLogin ? connectMultiPlatformScript : connectSinglePlatformScript;
        var result = await _database.ScriptEvaluateAsync(
            script,
            [RouteKey(route)],
            [
                route.Platform.ToRouteValue(),
                route.ConnectionId,
                checked((long)NormalizeTtl(ttl).TotalSeconds),
                route.ServerId,
            ]
        );

        var list = (RedisResult[])result!;
        if (list is null or [])
        {
            return null;
        }

        return ParseReplacedRoutes(list);
    }

    public async ValueTask<IReadOnlyCollection<DimUserConnectionRoute>> GetRoutesAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        if (userIds.Count == 0)
        {
            return [];
        }

        var normalizedUserIds = userIds
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Select(userId => userId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedUserIds.Length == 0)
        {
            return [];
        }

        var batch = _database.CreateBatch();
        var tasks = normalizedUserIds
            .Select(userId => batch.HashGetAllAsync(RouteKey(userId)))
            .ToArray();

        batch.Execute();

        await Task.WhenAll(tasks);

        var routes = new List<DimUserConnectionRoute>(normalizedUserIds.Length);
        for (var i = 0; i < normalizedUserIds.Length; i++)
        {
            AddUserRoutes(normalizedUserIds[i], tasks[i].Result, routes);
        }

        return routes;
    }

    public async ValueTask<bool> RemoveRouteAsync(
        DimConectionRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _database.ScriptEvaluateAsync(
            disconnectScript,
            [RouteKey(route)],
            [route.Platform.ToRouteValue(), route.ConnectionId]
        );
        return (int)result! > 0;
    }

    public async ValueTask<bool> RefreshRouteAsync(
        DimConectionRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        var result = await _database.ScriptEvaluateAsync(
            refreshScript,
            [RouteKey(route)],
            [
                route.Platform.ToRouteValue(),
                route.ConnectionId,
                checked((long)NormalizeTtl(ttl).TotalSeconds),
            ]
        );
        return (int)result! > 0;
    }

    public async Task RefreshTTLRoutesAsync(
        IEnumerable<DimConectionRoute> routes,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        var normalizedTtl = NormalizeTtl(ttl);
        var ttlSeconds = checked((long)normalizedTtl.TotalSeconds);

        var batch = _database.CreateBatch();

        var tasks = routes.Select(route => batch.ScriptEvaluateAsync(
                refreshScript,
                [RouteKey(route)],
                [
                    route.Platform.ToRouteValue(),
                    route.ConnectionId,
                    ttlSeconds,
                ]
            )
        ).ToArray();

        batch.Execute();

        await Task.WhenAll(tasks);
    }

    private string RouteKey(DimConectionRoute route)
    {
        return RouteKey(route.UserId);
    }

    private string RouteKey(string userId)
    {
        return $"{_routeKeyPrefix}:{{{userId}}}";
    }

    private static DimReplacedConnectionRoute[] ParseReplacedRoutes(RedisResult[] list)
    {
        var routes = new List<DimReplacedConnectionRoute>(list.Length / 3);
        for (var i = 0; i + 2 < list.Length; i += 3)
        {
            var platformValue = list[i].ToString();
            var connectionId = list[i + 1].ToString();
            var serverId = list[i + 2].ToString();

            if (DimClientPlatformParser.TryParse(platformValue, out var platform)
                && !string.IsNullOrWhiteSpace(connectionId)
                && !string.IsNullOrWhiteSpace(serverId))
            {
                routes.Add(new DimReplacedConnectionRoute(platform, connectionId!, serverId!));
            }
        }

        return [.. routes];
    }

    private static void AddUserRoutes(
        string userId,
        HashEntry[] entries,
        List<DimUserConnectionRoute> routes)
    {
        if (entries.Length == 0)
        {
            return;
        }

        var values = entries.ToDictionary(
            entry => entry.Name.ToString(),
            entry => entry.Value.ToString(),
            StringComparer.Ordinal);

        foreach (var (field, connectionId) in values)
        {
            if (!field.EndsWith("_c", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(connectionId))
            {
                continue;
            }

            var platformValue = field[..^2];
            if (!DimClientPlatformParser.TryParse(platformValue, out var platform)
                || !values.TryGetValue($"{platformValue}_s", out var serverId)
                || string.IsNullOrWhiteSpace(serverId))
            {
                continue;
            }

            routes.Add(new DimUserConnectionRoute(userId, platform, connectionId, serverId));
        }
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
