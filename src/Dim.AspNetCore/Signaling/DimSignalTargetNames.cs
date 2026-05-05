using System.Globalization;

namespace Dim.AspNetCore.Signaling;

internal static class DimSignalTargetNames
{
    public static string AppGroup(long appId)
    {
        return $"dim:app:{appId.ToString(CultureInfo.InvariantCulture)}";
    }

    public static string UserIdentifier(long appId, string userId)
    {
        return $"{appId.ToString(CultureInfo.InvariantCulture)}:{userId}";
    }
}
