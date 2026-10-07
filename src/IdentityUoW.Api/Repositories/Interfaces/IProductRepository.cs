using IdentityUoW.Api.Common.Repositories;
using IdentityUoW.Api.Entities;
using IdentityUoW.Api.Persistence;

namespace IdentityUoW.Api.Repositories.Interfaces;

public interface IProductRepository : IRepositoryBaseAsync<ProductEntity, Guid, AppDbContext>
{
    Task<ProductEntity?> GetProductByNoAsync(string productNo, CancellationToken cancellationToken = default);
}
