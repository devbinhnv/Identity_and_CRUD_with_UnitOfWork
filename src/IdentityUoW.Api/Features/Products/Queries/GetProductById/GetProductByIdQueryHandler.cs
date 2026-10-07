using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Dtos;
using IdentityUoW.Api.Repositories.Interfaces;

namespace IdentityUoW.Api.Features.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductByIdQuery, ApiResult<ProductDto>>
{
    public async Task<ApiResult<ProductDto>> HandleAsync(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken);

        return product is null
            ? new ApiErrorResult<ProductDto>($"Product '{request.Id}' was not found.", StatusCodes.Status404NotFound)
            : new ApiSuccessResult<ProductDto>(ProductDto.FromEntity(product));
    }
}
