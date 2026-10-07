using IdentityUoW.Api.Common.Repositories;
using IdentityUoW.Api.Entities;
using Microsoft.AspNetCore.Identity;

namespace IdentityUoW.Api.Persistence;

public static class AppDbContextSeed
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<RoleEntity>>();
        var userManager = services.GetRequiredService<UserManager<UserEntity>>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork<AppDbContext>>();

        // Seeding is an intentional exception with two commits: Identity looks roles up
        // in the database, so they must exist before a user can be added to one.
        foreach (var roleName in new[] { SystemRoles.Admin, SystemRoles.User })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new RoleEntity(roleName));
            }
        }

        await unitOfWork.CommitAsync();

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)
            || await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var admin = new UserEntity { UserName = email, Email = email, FullName = "Administrator" };
        var created = await userManager.CreateAsync(admin, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(admin, SystemRoles.Admin);
        await unitOfWork.CommitAsync();

        logger.LogInformation("Seeded administrator {Email}", email);
    }
}
