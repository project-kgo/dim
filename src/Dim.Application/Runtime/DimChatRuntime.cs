using Dim.Abstractions.Runtime;

namespace Dim.Application.Runtime;

public sealed class DimChatRuntime : IDimChatRuntime
{
    public DimChatRuntimeStatus GetStatus()
    {
        return new DimChatRuntimeStatus("Dim", "0.1.0", "Ready");
    }
}
