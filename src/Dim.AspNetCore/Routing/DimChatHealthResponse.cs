namespace Dim.AspNetCore.Routing;

public sealed record DimChatHealthResponse(
    string Name,
    string Version,
    string State);
