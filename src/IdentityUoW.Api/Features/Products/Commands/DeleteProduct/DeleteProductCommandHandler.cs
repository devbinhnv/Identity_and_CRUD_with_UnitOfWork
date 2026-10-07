using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Repositories.Interfaces;

namespace IdentityUoW.Api.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler(IProductRepository productRepository)
    : IRequestHandler<DeleteProductCommand, ApiResult<Guid>>
{
    public async Task<ApiResult<Guid>> HandleAsync(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return new ApiErrorResult<Guid>($"Product '{request.Id}' was not found.", StatusCodes.Status404NotFound);
        }

        await productRepository.DeleteAsync(product);

        return new ApiSuccessResult<Guid>(product.Id);
    }
}
