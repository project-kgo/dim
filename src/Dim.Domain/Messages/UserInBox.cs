
namespace Dim.Domain.Messages;


public enum UserBoxMessageType: int
{
   New = 0,
   Updated = 1,
   Revoked = 2,
   Deleted = 3,
}

public class UserInBox
{
    public required long Id { get; set; }

    public required long AppId { get; set; }

    public required string UserId { get; set; }

    public required string ConversationId { get; set; }

    public required int MessageType { get; set; }

    public required long MessageId { get; set; }

    public string? MessagePreview { get; set; }

    public required DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
