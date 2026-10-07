using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using IdentityUoW.Api.Common.Mediator;
using IdentityUoW.Api.Common.Repositories;
using IdentityUoW.Api.Configurations;
using IdentityUoW.Api.Entities;
using IdentityUoW.Api.Identity;
using IdentityUoW.Api.Persistence;
using IdentityUoW.Api.Repositories;
using IdentityUoW.Api.Repositories.Interfaces;
using IdentityUoW.Api.Services;
using IdentityUoW.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace IdentityUoW.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
        services.AddOpenApi();
        services.AddProblemDetails();

        services.ConfigureDbContext(configuration)
            .ConfigureIdentity()
            .ConfigureJwtAuthentication(configuration)
            .AddInfrastructureServices();

        services.AddMediator(Assembly.GetExecutingAssembly());

        return services;
    }

    private static IServiceCollection ConfigureDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnectionString")
            ?? throw new InvalidOperationException("Connection string isn't valid");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention());

        return services;
    }

    private static IServiceCollection ConfigureIdentity(this IServiceCollection services)
    {
        // AddIdentityCore + custom stores, and NO AddEntityFrameworkStores:
        // the non-auto-saving stores are the only IUserStore/IRoleStore registered.
        services.AddIdentityCore<UserEntity>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<RoleEntity>()
            .AddUserStore<UserStore>()
            .AddRoleStore<RoleStore>();

        return services;
    }

    private static IServiceCollection ConfigureJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(JwtSettings.SectionName);
        var settings = section.Get<JwtSettings>();
        if (settings is null || string.IsNullOrWhiteSpace(settings.SecretKey) || settings.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("JwtSettings:SecretKey must be at least 32 characters.");
        }

        services.Configure<JwtSettings>(section);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = settings.Issuer,
                ValidAudience = settings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            });

        services.AddAuthorization();

        return services;
    }

    private static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>))
                .AddScoped(typeof(IRepositoryBaseAsync<,,>), typeof(RepositoryBaseAsync<,,>))
                .AddScoped<IProductRepository, ProductRepository>()
                .AddScoped<IUserService, UserService>()
                .AddSingleton<ITokenService, TokenService>();

        return services;
    }
}
