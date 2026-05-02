using Dim.Contracts;

namespace Dim.Application.Signaling;

public interface IDimLocalSignalDispatcher
{
    ValueTask DispatchAsync(
        SignalEnvelope envelope,
        CancellationToken cancellationToken);
}
