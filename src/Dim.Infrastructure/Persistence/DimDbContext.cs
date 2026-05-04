using Microsoft.EntityFrameworkCore;

namespace Dim.Infrastructure.Persistence;

public class DimDbContext(DbContextOptions options) : DbContext(options);

public sealed class DimMasterDbContext(DbContextOptions<DimMasterDbContext> options) : DimDbContext(options);

public sealed class DimSlaveDbContext(DbContextOptions<DimSlaveDbContext> options) : DimDbContext(options);

