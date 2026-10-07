using Microsoft.EntityFrameworkCore;

namespace IdentityUoW.Api.Common.Repositories;

/// <summary>The single place that decides when tracked changes become data.</summary>
public interface IUnitOfWork<TContext>
    where TContext : DbContext
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}

/// <remarks>
/// The ONLY class that calls SaveChanges. One SaveChanges is already a transaction in EF Core,
/// so Identity changes and business changes are committed atomically.
/// </remarks>
public sealed class UnitOfWork<TContext>(TContext context) : IUnitOfWork<TContext>
    where TContext : DbContext
{
    public Task<int> CommitAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
