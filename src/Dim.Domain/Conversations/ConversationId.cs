namespace Dim.Domain.Conversations;

public readonly record struct ConversationId(string Value)
{
    public override string ToString() => Value;
}
