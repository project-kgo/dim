using Dim.Contracts;

namespace Dim.Application.Signaling;

public interface IDimLocalSignalDispatcher
{
    ValueTask DispatchAsync(
        SignalMessage signalMessage,
        CancellationToken cancellationToken);
}
