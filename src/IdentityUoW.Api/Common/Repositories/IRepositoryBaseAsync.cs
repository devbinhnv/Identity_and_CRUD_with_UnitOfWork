using System.Linq.Expressions;
using IdentityUoW.Api.Common.Domains;
using Microsoft.EntityFrameworkCore;

namespace IdentityUoW.Api.Common.Repositories;

/// <remarks>
/// Unlike a classic generic repository there is no SaveChangeAsync / transaction API here:
/// repositories only change tracked state, the Unit of Work commits.
/// </remarks>
public interface IRepositoryBaseAsync<TEntity, TKey, TContext>
    where TEntity : EntityBase<TKey>
    where TContext : DbContext
{
    IQueryable<TEntity> FindAll(bool trackChanges = false);

    IQueryable<TEntity> FindByCondition(Expression<Func<TEntity, bool>> expression, bool trackChanges = false);

    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task<TKey> CreateAsync(TEntity entity);

    Task UpdateAsync(TEntity entity);

    Task DeleteAsync(TEntity entity);
}
