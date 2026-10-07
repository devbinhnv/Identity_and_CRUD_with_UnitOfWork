using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Extensions;
using IdentityUoW.Api.Features.Auth.Commands.Register;
using IdentityUoW.Api.Features.Auth.Queries.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityUoW.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginQuery query, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(query, cancellationToken);
        return result.ToActionResult();
    }
}
