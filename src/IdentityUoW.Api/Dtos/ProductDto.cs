using System.ComponentModel.DataAnnotations;
using IdentityUoW.Api.Entities;

namespace IdentityUoW.Api.Dtos;

public record ProductDto(
    Guid Id,
    string No,
    string Name,
    string? Summary,
    decimal Price,
    Guid CreatedBy,
    DateTimeOffset CreatedDate,
    DateTimeOffset? LastModifiedDate)
{
    public static ProductDto FromEntity(ProductEntity entity) => new(
        entity.Id,
        entity.No,
        entity.Name,
        entity.Summary,
        entity.Price,
        entity.CreatedBy,
        entity.CreatedDate,
        entity.LastModifiedDate);
}

public record UpdateProductDto(
    [Required, MaxLength(250)] string Name,
    [MaxLength(500)] string? Summary,
    [Range(0, 9_999_999_999)] decimal Price);
