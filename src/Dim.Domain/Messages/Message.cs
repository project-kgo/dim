
using Dim.Abstractions.Messages;

namespace Dim.Domain.Messages;

public sealed class Message
{
    public required long Id { get; set; }

    public required long AppId { get; set; }

    public required string Sender { get; set; }

    public required string Receiver { get; set; }

    public required MessageContent Content { get; set; }

    public required DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public required DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public required DateTimeOffset DeletedAt { get; set; } = DateTimeOffset.UtcNow;
}
