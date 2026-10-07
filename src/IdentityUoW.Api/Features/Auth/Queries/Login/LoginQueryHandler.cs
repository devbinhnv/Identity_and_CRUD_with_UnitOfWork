using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Dtos;
using IdentityUoW.Api.Services.Interfaces;

namespace IdentityUoW.Api.Features.Auth.Queries.Login;

public class LoginQueryHandler(IUserService userService, ITokenService tokenService)
    : IRequestHandler<LoginQuery, ApiResult<TokenDto>>
{
    public async Task<ApiResult<TokenDto>> HandleAsync(LoginQuery request, CancellationToken cancellationToken)
    {
        var user = await userService.FindByCredentialsAsync(request.Email, request.Password);
        if (user is null)
        {
            return new ApiErrorResult<TokenDto>("The email or password is incorrect.", StatusCodes.Status401Unauthorized);
        }

        var roles = await userService.GetRolesAsync(user);
        return new ApiSuccessResult<TokenDto>(tokenService.GenerateToken(user, roles));
    }
}
