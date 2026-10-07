using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;

namespace IdentityUoW.Api.Features.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid Id) : ICommand<ApiResult<Guid>>;
