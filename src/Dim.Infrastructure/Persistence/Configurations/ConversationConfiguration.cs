using Dim.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dim.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration: IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations", "dim");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").IsRequired();
        builder.Property(x => x.AppId).HasColumnName("app_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.ConversationId)
            .HasColumnName("conversation_id")
            .HasConversion(x => x.Value, x => new ConversationId(x))
            .IsRequired();
        builder.Property(x => x.ConversationType).HasColumnName("conversation_type").HasConversion<int>().IsRequired();
        builder.Property(x => x.LastMessageId).HasColumnName("last_message_id").IsRequired();
        builder.Property(x => x.LastMessageAt).HasColumnName("last_message_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.LastMessagePreview).HasColumnName("last_message_preview");
        builder.Property(x => x.UnreadCount).HasColumnName("unread_count").IsRequired();
        builder.Property(x => x.DeletedBeforeMessageId).HasColumnName("deleted_before_message_id").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.AppId, x.UserId, x.LastMessageAt });
        builder.HasIndex(x => new { x.AppId, x.UserId, x.ConversationId }).IsUnique();
    }
}
