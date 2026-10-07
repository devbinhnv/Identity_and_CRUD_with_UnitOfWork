using IdentityUoW.Api.Common.Domains;
using IdentityUoW.Api.Entities;
using IdentityUoW.Api.Persistence.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityUoW.Api.Persistence;

/// <summary>
/// One request-scoped context for Identity AND business tables, so a single SaveChanges
/// (called only by the Unit of Work) commits both atomically.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<UserEntity, RoleEntity, Guid>(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureIdentityTables();
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        RenameForeignKeys(builder);
    }

    /// <summary>Audit columns are filled here, so no handler has to remember it.</summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IDateTracking>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedDate = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Identity creates its relationships while the tables are still "AspNet*", which would leave
    /// names like fk_user_claims_asp_net_users_user_id. Rebuild them from the final table names.
    /// </summary>
    private static void RenameForeignKeys(ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            foreach (var fk in entity.GetForeignKeys())
            {
                var columns = string.Join('_', fk.Properties.Select(p => p.GetColumnName()));
                fk.SetConstraintName($"fk_{entity.GetTableName()}_{fk.PrincipalEntityType.GetTableName()}_{columns}");
            }
        }
    }
}
