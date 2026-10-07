using System.Linq.Expressions;
using IdentityUoW.Api.Common.Domains;
using Microsoft.EntityFrameworkCore;

namespace IdentityUoW.Api.Common.Repositories;

public class RepositoryBaseAsync<TEntity, TKey, TContext>(TContext dbContext)
    : IRepositoryBaseAsync<TEntity, TKey, TContext>
    where TEntity : EntityBase<TKey>
    where TContext : DbContext
{
    protected TContext DbContext { get; } = dbContext;

    #region Query Operations
    public IQueryable<TEntity> FindAll(bool trackChanges = false) =>
        trackChanges ? DbContext.Set<TEntity>() : DbContext.Set<TEntity>().AsNoTracking();

    public IQueryable<TEntity> FindByCondition(Expression<Func<TEntity, bool>> expression, bool trackChanges = false) =>
        FindAll(trackChanges).Where(expression);

    /// <remarks>FindAsync looks in the ChangeTracker first, so entities added in the same Unit of Work are found.</remarks>
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default) =>
        await DbContext.Set<TEntity>().FindAsync([id], cancellationToken);
    #endregion

    #region CRUD Operations (tracked only, no SaveChanges)
    public Task<TKey> CreateAsync(TEntity entity)
    {
        DbContext.Set<TEntity>().Add(entity);
        return Task.FromResult(entity.Id);
    }

    public Task UpdateAsync(TEntity entity)
    {
        // Tracked entities are detected automatically; only detached ones need Update().
        if (DbContext.Entry(entity).State == EntityState.Detached)
        {
            DbContext.Set<TEntity>().Update(entity);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity)
    {
        DbContext.Set<TEntity>().Remove(entity);
        return Task.CompletedTask;
    }
    #endregion
}
