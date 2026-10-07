using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Entities;
using IdentityUoW.Api.Repositories.Interfaces;

namespace IdentityUoW.Api.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandler(IProductRepository productRepository)
    : IRequestHandler<CreateProductCommand, ApiResult<Guid>>
{
    public async Task<ApiResult<Guid>> HandleAsync(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await productRepository.GetProductByNoAsync(request.No, cancellationToken) is not null)
        {
            return new ApiErrorResult<Guid>($"Product No: {request.No} already exists.", StatusCodes.Status409Conflict);
        }

        var product = new ProductEntity
        {
            Id = Guid.CreateVersion7(),
            No = request.No,
            Name = request.Name,
            Summary = request.Summary,
            Price = request.Price,
            CreatedBy = request.CreatedBy
        };

        await productRepository.CreateAsync(product);

        return new ApiSuccessResult<Guid>(product.Id);
    }
}
