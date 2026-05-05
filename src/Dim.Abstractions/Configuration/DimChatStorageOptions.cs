namespace Dim.Abstractions.Configuration;

public sealed class DimChatStorageOptions
{
    public string? PgMasterSqlConnectionString { get; set; }

    public string? PgSlaveSqlConnectionString { get; set; }

    public string? RedisConnectionString { get; set; }

    public string RedisStreamName { get; set; } = "dim:messages";

    public string CapStorageSchema { get; set; } = "cap";

    public string CapDefaultGroupName { get; set; } = "dim";
}
