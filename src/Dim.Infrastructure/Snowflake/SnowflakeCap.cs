using DotNetCore.CAP.Internal;
using Ku.Utils.Snowflake;

namespace Dim.Infrastructure.Snowflake;

public class SnowflakeCap(DistributedSnowflake distributedSnowflake) : ISnowflakeId
{
    public long NextId() => distributedSnowflake.Generate();
}
