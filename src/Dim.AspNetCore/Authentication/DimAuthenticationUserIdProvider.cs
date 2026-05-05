using Dim.Abstractions.Authentication;
using Dim.AspNetCore.Signaling;
using Microsoft.AspNetCore.SignalR;
using System.Globalization;

namespace Dim.AspNetCore.Authentication;

public sealed class DimAuthenticationUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var appIdValue = connection.User?.FindFirst(AuthConstants.AppIdClaim)?.Value;
        var userId = connection.User?.FindFirst(AuthConstants.UserIdClaim)?.Value;
        if (!long.TryParse(appIdValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var appId)
            || string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return DimSignalTargetNames.UserIdentifier(appId, userId);
    }
}
