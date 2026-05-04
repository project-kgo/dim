using Dim.Domain.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dim.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration: IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("dim.Messages");
        builder.HasKey(x => new {x.Id, x.CreatedAt});

        builder.Property(x => x.Id).HasColumnName("id").IsRequired();
        builder.Property(x => x.AppId).HasColumnName("app_id").IsRequired();
        builder.Property(x => x.Sender).HasColumnName("sender").IsRequired();
        builder.Property(x => x.Receiver).HasColumnName("receiver").IsRequired();
        builder.Property(x => x.Content).HasColumnName("content").IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(x => new { x.Id, x.CreatedAt });
    }
}
