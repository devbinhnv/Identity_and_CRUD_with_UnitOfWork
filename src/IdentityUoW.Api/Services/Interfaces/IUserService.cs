using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Entities;

namespace IdentityUoW.Api.Services.Interfaces;

/// <summary>
/// Thin wrapper over UserManager (instead of a fake "UserRepository").
/// Every write is tracked only; nothing here calls SaveChanges.
/// </summary>
public interface IUserService
{
    Task<ApiResult<Guid>> CreateAsync(string email, string password, string? fullName);

    Task<ApiResult<Guid>> AssignRoleAsync(Guid userId, string roleName);

    Task<UserEntity?> FindByCredentialsAsync(string email, string password);

    Task<IList<string>> GetRolesAsync(UserEntity user);
}
