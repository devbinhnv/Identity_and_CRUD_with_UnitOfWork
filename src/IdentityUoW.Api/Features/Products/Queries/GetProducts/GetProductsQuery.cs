using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Dtos;

namespace IdentityUoW.Api.Features.Products.Queries.GetProducts;

public class GetProductsQuery : PagingRequestParameters, IRequest<ApiResult<PageList<ProductDto>>>
{
    public string? Search { get; set; }
}
