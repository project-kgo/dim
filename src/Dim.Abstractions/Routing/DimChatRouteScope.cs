namespace Dim.Abstractions.Routing;

public sealed record DimChatRouteScope(
    string UserId,
    DimClientPlatform? Platform,
    string Key)
{
    public static DimChatRouteScope ForUser(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new DimChatRouteScope(userId, null, $"user:{userId}");
    }

    public static DimChatRouteScope ForPlatform(string userId, DimClientPlatform platform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new DimChatRouteScope(userId, platform, $"user:{userId}:platform:{platform.ToRouteValue()}");
    }
}
