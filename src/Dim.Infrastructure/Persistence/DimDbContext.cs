using Microsoft.EntityFrameworkCore;

namespace Dim.Infrastructure.Persistence;

public sealed class DimDbContext(DbContextOptions<DimDbContext> options) : DbContext(options)
{
}
