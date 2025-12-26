using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IncidentScope.Data.Postgres;

public class IncidentScopeDbContext : DbContext
{
    public IncidentScopeDbContext(DbContextOptions<IncidentScopeDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure UUID generation
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.HasPostgresExtension("vector");

        // Configure entities with tenant isolation
        ConfigureTenantEntities(modelBuilder);
    }

    private static void ConfigureTenantEntities(ModelBuilder modelBuilder)
    {
        // All entities should have tenant_id indexed
        // This is handled via conventions in actual entity configurations
    }
}

