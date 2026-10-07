using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Dtos;

namespace IdentityUoW.Api.Features.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ApiResult<ProductDto>>;
