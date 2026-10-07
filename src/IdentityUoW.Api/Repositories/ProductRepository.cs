using IdentityUoW.Api.Common.Repositories;
using IdentityUoW.Api.Entities;
using IdentityUoW.Api.Persistence;
using IdentityUoW.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IdentityUoW.Api.Repositories;

public class ProductRepository(AppDbContext dbContext)
    : RepositoryBaseAsync<ProductEntity, Guid, AppDbContext>(dbContext), IProductRepository
{
    public Task<ProductEntity?> GetProductByNoAsync(string productNo, CancellationToken cancellationToken = default) =>
        FindByCondition(p => p.No == productNo).SingleOrDefaultAsync(cancellationToken);
}
