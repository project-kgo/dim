namespace Dim.Abstractions.Configuration;

public sealed class DimChatStorageOptions
{
    public string? PostgreSqlConnectionString { get; set; }

    public string? RedisConnectionString { get; set; }

    public string RedisStreamName { get; set; } = "dim:messages";
}
