namespace IdentityUoW.Api.Extensions;

public static class ApplicationExtensions
{
    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
