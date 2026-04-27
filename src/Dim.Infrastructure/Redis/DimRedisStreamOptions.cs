namespace Dim.Infrastructure.Redis;

public sealed class DimRedisStreamOptions
{
    public string StreamName { get; init; } = "dim:messages";
}
