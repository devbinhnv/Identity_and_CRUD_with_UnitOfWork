using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Repositories.Interfaces;

namespace IdentityUoW.Api.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler(IProductRepository productRepository)
    : IRequestHandler<UpdateProductCommand, ApiResult<Guid>>
{
    public async Task<ApiResult<Guid>> HandleAsync(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return new ApiErrorResult<Guid>($"Product '{request.Id}' was not found.", StatusCodes.Status404NotFound);
        }

        product.Name = request.Product.Name;
        product.Summary = request.Product.Summary;
        product.Price = request.Product.Price;

        await productRepository.UpdateAsync(product);

        return new ApiSuccessResult<Guid>(product.Id);
    }
}
