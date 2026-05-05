
using Dim.Abstractions.Routing;

namespace Dim.Abstractions.Authentication;

public readonly record struct LoginId(string Value)
{
    public LoginId(string userId, DimClientPlatform platform)
        : this($"{userId}:{platform}")
    {
    }

    public override string ToString() => Value;
}
