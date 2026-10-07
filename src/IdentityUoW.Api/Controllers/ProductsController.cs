using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Dtos;
using IdentityUoW.Api.Extensions;
using IdentityUoW.Api.Features.Products.Commands.CreateProduct;
using IdentityUoW.Api.Features.Products.Commands.DeleteProduct;
using IdentityUoW.Api.Features.Products.Commands.UpdateProduct;
using IdentityUoW.Api.Features.Products.Queries.GetProductById;
using IdentityUoW.Api.Features.Products.Queries.GetProducts;
using IdentityUoW.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityUoW.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] GetProductsQuery query, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new GetProductByIdQuery(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(command with { CreatedBy = User.GetUserId() }, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductDto product, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new UpdateProductCommand(id, product), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new DeleteProductCommand(id), cancellationToken);
        return result.ToActionResult();
    }
}
