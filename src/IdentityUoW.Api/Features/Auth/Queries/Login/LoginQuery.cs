using System.ComponentModel.DataAnnotations;
using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Dtos;

namespace IdentityUoW.Api.Features.Auth.Queries.Login;

/// <summary>
/// A query: it only checks the password (no lockout counter is written), so nothing to commit.
/// </summary>
public record LoginQuery(
    [Required, EmailAddress] string Email,
    [Required] string Password) : IRequest<ApiResult<TokenDto>>;
