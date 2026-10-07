using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Entities;
using IdentityUoW.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace IdentityUoW.Api.Services;

public class UserService(UserManager<UserEntity> userManager, RoleManager<RoleEntity> roleManager) : IUserService
{
    public async Task<ApiResult<Guid>> CreateAsync(string email, string password, string? fullName)
    {
        var user = new UserEntity { UserName = email, Email = email, FullName = fullName };

        var result = await userManager.CreateAsync(user, password);

        // Succeeded = accepted and tracked. Not committed yet.
        return result.Succeeded ? new ApiSuccessResult<Guid>(user.Id) : ToError<Guid>(result);
    }

    public async Task<ApiResult<Guid>> AssignRoleAsync(Guid userId, string roleName)
    {
        // FindByIdAsync checks the ChangeTracker first: a user created earlier in this
        // Unit of Work (not committed yet) is found.
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new ApiErrorResult<Guid>($"User '{userId}' was not found.", StatusCodes.Status404NotFound);
        }

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            return new ApiErrorResult<Guid>($"Role '{roleName}' was not found.", StatusCodes.Status404NotFound);
        }

        var result = await userManager.AddToRoleAsync(user, roleName);
        return result.Succeeded ? new ApiSuccessResult<Guid>(user.Id) : ToError<Guid>(result);
    }

    public async Task<UserEntity?> FindByCredentialsAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);

        return user is { IsActive: true } && await userManager.CheckPasswordAsync(user, password) ? user : null;
    }

    public Task<IList<string>> GetRolesAsync(UserEntity user) => userManager.GetRolesAsync(user);

    private static ApiErrorResult<T> ToError<T>(IdentityResult result)
    {
        var isConflict = result.Errors.Any(e => e.Code.StartsWith("Duplicate", StringComparison.Ordinal));

        return new ApiErrorResult<T>(
            result.Errors.Select(e => e.Description),
            isConflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest);
    }
}
