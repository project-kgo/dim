using Ku.Utils.Snowflake;
using System.Globalization;

namespace Dim.Application.Routing;


public sealed class DimServerIdentity(DistributedSnowflake snowflake)
{
    public string ServerId { get; } = snowflake.Generate().ToString(CultureInfo.InvariantCulture);
}
