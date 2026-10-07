using Microsoft.EntityFrameworkCore;

namespace IdentityUoW.Api.Extensions;

public static class HostExtensions
{
    public static async Task<IHost> MigrateDatabaseAsync<TContext>(
        this IHost host,
        Func<TContext, IServiceProvider, Task> seeder)
        where TContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<TContext>>();
        var context = services.GetRequiredService<TContext>();

        try
        {
            logger.LogInformation("Migrating database.");
            await context.Database.MigrateAsync();
            logger.LogInformation("Migrated database.");

            await seeder(context, services);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }

        return host;
    }
}
