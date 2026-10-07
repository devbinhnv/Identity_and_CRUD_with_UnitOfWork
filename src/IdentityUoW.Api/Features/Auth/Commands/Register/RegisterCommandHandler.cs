using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Entities;
using IdentityUoW.Api.Services.Interfaces;

namespace IdentityUoW.Api.Features.Auth.Commands.Register;

/// <summary>
/// identity.users + identity.user_roles are tracked here and committed together by the
/// mediator. If assigning the role fails, the user is never persisted.
/// </summary>
public class RegisterCommandHandler(IUserService userService) : IRequestHandler<RegisterCommand, ApiResult<Guid>>
{
    public async Task<ApiResult<Guid>> HandleAsync(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(request.Email, request.Password, request.FullName);
        if (!user.IsSucceeded)
        {
            return user;
        }

        // No SaveChanges anywhere: the mediator commits once, after this handler succeeds.
        return await userService.AssignRoleAsync(user.Data, SystemRoles.User);
    }
}
