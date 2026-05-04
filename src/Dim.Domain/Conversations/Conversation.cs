using Dim.Abstractions.Conversations;

namespace Dim.Domain.Conversations;



public sealed class Conversation
{
    public required long Id { get; set; }

    public required long AppId { get; set; }

    public required string UserId { get; set; }

    public required string ConversationId { get; set; }

    public required ConversationType ConversationType
    { get; set; }

    public required long LastMessageId { get; set; }

    public required int UnreadCount { get; set; }

    public required long DeletedBeforeMessageId { get; set; }

    public required DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public required DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
