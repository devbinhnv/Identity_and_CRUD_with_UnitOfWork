using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Dtos;
using IdentityUoW.Api.Repositories.Interfaces;

namespace IdentityUoW.Api.Features.Products.Queries.GetProducts;

public class GetProductsQueryHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductsQuery, ApiResult<PageList<ProductDto>>>
{
    public async Task<ApiResult<PageList<ProductDto>>> HandleAsync(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = productRepository.FindAll();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(p => p.Name.Contains(request.Search) || p.No.Contains(request.Search));
        }

        var page = await PageList<ProductDto>.ToPagedListAsync(
            query.OrderBy(p => p.No).Select(p => ProductDto.FromEntity(p)),
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return new ApiSuccessResult<PageList<ProductDto>>(page);
    }
}
