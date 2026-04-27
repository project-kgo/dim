using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Globalization;

namespace Dim.Infrastructure.Redis;

public sealed class RedisDimChatRouteStore : IDimChatRouteStore
{
    private const string ScopeKeyField = "scopeKey";
    private const string UserIdField = "userId";
    private const string PlatformField = "platform";
    private const string ConnectionIdField = "connectionId";
    private const string ConnectedAtUtcField = "connectedAtUtc";
    private const string EmptyPlatformValue = "-";

    private const string RemoveIfCurrentScript = """
        local currentConnectionId = redis.call('HGET', KEYS[1], 'connectionId')
        if currentConnectionId == ARGV[1] then
            redis.call('DEL', KEYS[1])
            redis.call('DEL', KEYS[2])
            return 1
        end

        redis.call('DEL', KEYS[2])
        return 0
        """;

    private const string RefreshIfCurrentScript = """
        local currentConnectionId = redis.call('HGET', KEYS[1], 'connectionId')
        if currentConnectionId == ARGV[1] then
            redis.call('PEXPIRE', KEYS[1], ARGV[2])
            redis.call('PEXPIRE', KEYS[2], ARGV[2])
            return 1
        end

        return 0
        """;

    private readonly IDatabase _database;
    private readonly string _routeKeyPrefix;

    public RedisDimChatRouteStore(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<DimChatOptions> options)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(options);

        _database = connectionMultiplexer.GetDatabase();
        _routeKeyPrefix = NormalizePrefix(options.Value.Connection.RouteKeyPrefix);
    }

    public async ValueTask<DimChatRoute?> GetRouteAsync(
        DimChatRouteScope scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var values = await _database.HashGetAllAsync(RouteKey(scope));
        if (values.Length == 0)
        {
            return null;
        }

        return CreateRoute(scope, values);
    }

    public async ValueTask SetRouteAsync(
        DimChatRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(route);

        var routeKey = RouteKey(route.Scope);
        var connectionKey = ConnectionKey(route.ConnectionId);
        var expiry = NormalizeTtl(ttl);
        var entries = new[]
        {
            new HashEntry(ScopeKeyField, route.Scope.Key),
            new HashEntry(UserIdField, route.UserId),
            new HashEntry(PlatformField, route.Platform.ToRouteValue()),
            new HashEntry(ConnectionIdField, route.ConnectionId),
            new HashEntry(ConnectedAtUtcField, route.ConnectedAtUtc.ToUnixTimeMilliseconds())
        };
        var connectionEntries = new[]
        {
            new HashEntry(ScopeKeyField, route.Scope.Key),
            new HashEntry(UserIdField, route.UserId),
            new HashEntry(PlatformField, route.Scope.Platform?.ToRouteValue() ?? EmptyPlatformValue)
        };

        await _database.HashSetAsync(routeKey, entries);
        await _database.KeyExpireAsync(routeKey, expiry);
        await _database.HashSetAsync(connectionKey, connectionEntries);
        await _database.KeyExpireAsync(connectionKey, expiry);
    }

    public async ValueTask<DimChatRouteScope?> GetRouteScopeAsync(
        string connectionId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var values = await _database.HashGetAllAsync(ConnectionKey(connectionId));
        if (values.Length == 0)
        {
            return null;
        }

        return CreateScope(values);
    }

    public async ValueTask<bool> RemoveRouteIfCurrentAsync(
        DimChatRouteScope scope,
        string connectionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var result = await _database.ScriptEvaluateAsync(
            RemoveIfCurrentScript,
            new RedisKey[] { RouteKey(scope), ConnectionKey(connectionId) },
            new RedisValue[] { connectionId });

        return (int)result == 1;
    }

    public async ValueTask<bool> RefreshRouteAsync(
        DimChatRouteScope scope,
        string connectionId,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var result = await _database.ScriptEvaluateAsync(
            RefreshIfCurrentScript,
            [RouteKey(scope), ConnectionKey(connectionId)],
            [connectionId, checked((long)NormalizeTtl(ttl).TotalMilliseconds)]);

        return (int)result == 1;
    }

    private RedisKey RouteKey(DimChatRouteScope scope)
    {
        return $"{_routeKeyPrefix}:{scope.Key}";
    }

    private RedisKey ConnectionKey(string connectionId)
    {
        return $"{_routeKeyPrefix}:connections:{connectionId}";
    }

    private static DimChatRoute CreateRoute(DimChatRouteScope requestedScope, HashEntry[] values)
    {
        var map = ToMap(values);
        var userId = GetRequired(map, UserIdField);
        var platformValue = GetRequired(map, PlatformField);
        if (!DimClientPlatformParser.TryParse(platformValue, out var platform))
        {
            throw new InvalidOperationException("Redis 用户路由中的客户端平台无效。");
        }

        var connectionId = GetRequired(map, ConnectionIdField);
        var connectedAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(
            long.Parse(GetRequired(map, ConnectedAtUtcField), CultureInfo.InvariantCulture));

        return new DimChatRoute(requestedScope, userId, platform, connectionId, connectedAtUtc);
    }

    private static DimChatRouteScope CreateScope(HashEntry[] values)
    {
        var map = ToMap(values);
        var userId = GetRequired(map, UserIdField);
        var platformValue = GetRequired(map, PlatformField);

        return platformValue == EmptyPlatformValue
            ? DimChatRouteScope.ForUser(userId)
            : DimChatRouteScope.ForPlatform(userId, ParsePlatform(platformValue));
    }

    private static DimClientPlatform ParsePlatform(string value)
    {
        return DimClientPlatformParser.TryParse(value, out var platform)
            ? platform
            : throw new InvalidOperationException("Redis 连接索引中的客户端平台无效。");
    }

    private static Dictionary<string, string> ToMap(HashEntry[] values)
    {
        return values.ToDictionary(
            static entry => entry.Name.ToString(),
            static entry => entry.Value.ToString(),
            StringComparer.Ordinal);
    }

    private static string GetRequired(Dictionary<string, string> map, string fieldName)
    {
        return map.TryGetValue(fieldName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Redis 用户路由缺少 {fieldName} 字段。");
    }

    private static string NormalizePrefix(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "dim:routes" : value.Trim();
        return normalized.Trim(':');
    }

    private static TimeSpan NormalizeTtl(TimeSpan ttl)
    {
        return ttl <= TimeSpan.Zero ? TimeSpan.FromDays(7) : ttl;
    }
}
