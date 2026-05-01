using Dim.Abstractions.Authentication;
using Microsoft.AspNetCore.SignalR;

namespace Dim.AspNetCore.Authentication;

public sealed class DimAuthenticationUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(AuthConstants.UserIdClaim)?.Value;
    }
}
