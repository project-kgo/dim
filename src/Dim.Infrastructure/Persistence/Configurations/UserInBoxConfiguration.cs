using Dim.Domain.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dim.Infrastructure.Persistence.Configurations;

public sealed class UserInBoxConfiguration: IEntityTypeConfiguration<UserInBox>
{
    public void Configure(EntityTypeBuilder<UserInBox> builder)
    {
        builder.ToTable("user_in_boxes", "dim");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").IsRequired();
        builder.Property(x => x.AppId).HasColumnName("app_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.ConversationId).HasColumnName("conversation_id").IsRequired();
        builder.Property(x => x.MessageType).HasColumnName("message_type").IsRequired();
        builder.Property(x => x.MessageId).HasColumnName("message_id").IsRequired();
        builder.Property(x => x.MessagePreview).HasColumnName("message_preview");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.AppId, x.UserId, x.CreatedAt });
        builder.HasIndex(x => new { x.AppId, x.UserId, x.ConversationId, x.MessageId });
    }
}
