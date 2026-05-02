namespace Dim.Abstractions.Configuration;

public sealed class DimChatSignalingOptions
{
    public string RedisChannel { get; set; } = "dim:signals";

    public string ClientMethodName { get; set; } = "ReceiveSignal";
}
