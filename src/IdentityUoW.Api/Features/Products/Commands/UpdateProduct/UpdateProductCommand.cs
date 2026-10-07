using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Dtos;

namespace IdentityUoW.Api.Features.Products.Commands.UpdateProduct;

public record UpdateProductCommand(Guid Id, UpdateProductDto Product) : ICommand<ApiResult<Guid>>;
