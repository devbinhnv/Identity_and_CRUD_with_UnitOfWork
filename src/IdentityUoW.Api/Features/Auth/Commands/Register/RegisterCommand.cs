using System.ComponentModel.DataAnnotations;
using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;

namespace IdentityUoW.Api.Features.Auth.Commands.Register;

public record RegisterCommand(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [MaxLength(200)] string? FullName) : ICommand<ApiResult<Guid>>;
