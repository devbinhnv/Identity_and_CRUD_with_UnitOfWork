using IdentityUoW.Api.Extensions;
using IdentityUoW.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseInfrastructure();

await app.MigrateDatabaseAsync<AppDbContext>((_, services) =>
    AppDbContextSeed.SeedAsync(services, app.Configuration, app.Logger));

await app.RunAsync();
