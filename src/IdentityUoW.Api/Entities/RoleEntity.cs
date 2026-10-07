using Microsoft.AspNetCore.Identity;

namespace IdentityUoW.Api.Entities;

public class RoleEntity : IdentityRole<Guid>
{
    public RoleEntity()
    {
        Id = Guid.CreateVersion7();
    }

    public RoleEntity(string roleName) : this()
    {
        Name = roleName;
    }
}

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
