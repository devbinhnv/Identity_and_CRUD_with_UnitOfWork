using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;

namespace IdentityUoW.Api.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(
    [Required, MaxLength(50)] string No,
    [Required, MaxLength(250)] string Name,
    [MaxLength(500)] string? Summary,
    [Range(0, 9_999_999_999)] decimal Price) : ICommand<ApiResult<Guid>>
{
    /// <summary>Set by the controller from the JWT, never from the request body.</summary>
    [JsonIgnore]
    public Guid CreatedBy { get; init; }
}
