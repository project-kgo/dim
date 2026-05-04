using Microsoft.EntityFrameworkCore;
using Dim.Domain.Messages;
namespace Dim.Infrastructure.Persistence;

public class DimDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Message> Messages => Set<Message>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DimDbContext).Assembly);
    }
}

public sealed class DimMasterDbContext(DbContextOptions<DimMasterDbContext> options) : DimDbContext(options);

public sealed class DimSlaveDbContext(DbContextOptions<DimSlaveDbContext> options) : DimDbContext(options);

