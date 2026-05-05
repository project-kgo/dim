
using Dim.Abstractions.Routing;

namespace Dim.Abstractions.Authentication;

public readonly record struct LoginId(string Value)
{
    public LoginId(long appId, string userId, DimClientPlatform platform)
        : this($"{appId}:{userId}:{platform}")
    {
    }

    public override string ToString() => Value;
}
