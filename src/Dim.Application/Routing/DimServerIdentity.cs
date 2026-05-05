using Ku.Utils.Snowflake;
using System.Globalization;

namespace Dim.Application.Routing;


public sealed class DimServerIdentity
{
    public DimServerIdentity()
    {
        ServerId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
    }

    public DimServerIdentity(DistributedSnowflake snowflake)
    {
        ServerId = snowflake.Generate().ToString(CultureInfo.InvariantCulture);
    }

    public string ServerId { get; }
}
