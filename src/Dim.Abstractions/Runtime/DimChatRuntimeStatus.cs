namespace Dim.Abstractions.Runtime;

public sealed record DimChatRuntimeStatus(
    string Name,
    string Version,
    string State);
