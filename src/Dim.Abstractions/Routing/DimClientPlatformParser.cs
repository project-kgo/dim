namespace Dim.Abstractions.Routing;

public static class DimClientPlatformParser
{
    public static bool TryParse(string? value, out DimClientPlatform platform)
    {
        platform = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        platform = value.Trim().ToLowerInvariant() switch
        {
            "ios" => DimClientPlatform.Ios,
            "android" => DimClientPlatform.Android,
            "web" => DimClientPlatform.Web,
            _ => default
        };

        return platform is not default(DimClientPlatform);
    }

    public static string ToRouteValue(this DimClientPlatform platform)
    {
        return platform switch
        {
            DimClientPlatform.Ios => "ios",
            DimClientPlatform.Android => "android",
            DimClientPlatform.Web => "web",
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, "不支持的客户端平台。")
        };
    }
}
