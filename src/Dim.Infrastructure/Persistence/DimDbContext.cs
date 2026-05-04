using Microsoft.EntityFrameworkCore;
using Dim.Domain.Messages;
using Dim.Domain.Conversations;
namespace Dim.Infrastructure.Persistence;

public abstract class DimDbContextBase(DbContextOptions options) : DbContext(options)
{
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<UserInBox> UserInBoxes => Set<UserInBox>();
    public DbSet<UserMessageState> UserMessageStates => Set<UserMessageState>();
    public DbSet<Conversation> Conversations => Set<Conversation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DimDbContextBase).Assembly);
    }
}

// public class DimDbContext(DbContextOptions<DimDbContext> options) : DbContext(options)
// {
//     public DbSet<Message> Messages => Set<Message>();
//     protected override void OnModelCreating(ModelBuilder modelBuilder)
//     {
//         base.OnModelCreating(modelBuilder);
//         modelBuilder.ApplyConfigurationsFromAssembly(typeof(DimDbContext).Assembly);
//     }
// }

public sealed class DimMasterDbContext(DbContextOptions<DimMasterDbContext> options) : DimDbContextBase(options);

public sealed class DimSlaveDbContext(DbContextOptions<DimSlaveDbContext> options) : DimDbContextBase(options);

