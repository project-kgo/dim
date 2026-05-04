using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dim.Domain.Messages;

public class UserMessageState
{
    public required long Id { get; set; }

    public required long AppId { get; set; }

    public required string UserId { get; set; }

    public required string ConversationId { get; set; }

    public required long MessageId { get; set; }

    public required int State { get; set; }

    public required DateTimeOffset DeletedAt { get; set; } = DateTimeOffset.UtcNow;
}