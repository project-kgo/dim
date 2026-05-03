namespace Dim.Application.Routing;

public sealed class DimServerIdentity
{
    public string ServerId { get; } = Guid.NewGuid().ToString("N");
}
