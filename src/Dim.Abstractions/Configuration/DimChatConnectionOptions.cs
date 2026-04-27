namespace Dim.Abstractions.Configuration;

public sealed class DimChatConnectionOptions
{
    public bool AllowMultiDeviceLogin { get; set; } = true;

    public TimeSpan RouteTtl { get; set; } = TimeSpan.FromDays(7);

    public string RouteKeyPrefix { get; set; } = "dim:routes";
}
