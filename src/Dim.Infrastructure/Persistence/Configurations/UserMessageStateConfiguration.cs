using Dim.Domain.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dim.Infrastructure.Persistence.Configurations;

public sealed class UserMessageStateConfiguration: IEntityTypeConfiguration<UserMessageState>
{
    public void Configure(EntityTypeBuilder<UserMessageState> builder)
    {
        builder.ToTable("user_message_states", "dim");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").IsRequired();
        builder.Property(x => x.AppId).HasColumnName("app_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.ConversationId).HasColumnName("conversation_id").IsRequired();
        builder.Property(x => x.MessageId).HasColumnName("message_id").IsRequired();
        builder.Property(x => x.State).HasColumnName("state").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.AppId, x.UserId, x.ConversationId, x.MessageId }).IsUnique();
        builder.HasIndex(x => new { x.AppId, x.UserId, x.State });
    }
}
