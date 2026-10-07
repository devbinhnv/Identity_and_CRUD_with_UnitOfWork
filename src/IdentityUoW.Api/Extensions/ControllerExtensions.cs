using System.Security.Claims;
using IdentityUoW.Api.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityUoW.Api.Extensions;

public static class ControllerExtensions
{
    public static IActionResult ToActionResult(this ApiResult result) =>
        new ObjectResult(result) { StatusCode = result.StatusCode };

    /// <summary>"sub" is mapped to NameIdentifier by the JWT bearer handler.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The token has no subject claim."));
}
