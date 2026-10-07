using IdentityUoW.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityUoW.Api.Persistence.Configurations;

/// <summary>
/// Moves every ASP.NET Core Identity table out of "AspNet*" into the <c>identity</c> schema:
/// <code>
/// AspNetUsers      -> identity.users
/// AspNetRoles      -> identity.roles
/// AspNetUserRoles  -> identity.user_roles
/// AspNetUserClaims -> identity.user_claims
/// AspNetUserLogins -> identity.user_logins
/// AspNetUserTokens -> identity.user_tokens
/// AspNetRoleClaims -> identity.role_claims
/// </code>
/// </summary>
public static class IdentityConfiguration
{
    public static ModelBuilder ConfigureIdentityTables(this ModelBuilder builder)
    {
        builder.Entity<UserEntity>(b =>
        {
            b.ToTable("users", Schemas.Identity);
            b.Property(u => u.Id).ValueGeneratedNever();
            b.Property(u => u.FullName).HasMaxLength(200);
            b.HasIndex(u => u.NormalizedUserName).HasDatabaseName("ix_users_normalized_user_name").IsUnique();
            b.HasIndex(u => u.NormalizedEmail).HasDatabaseName("ix_users_normalized_email").IsUnique();
        });

        builder.Entity<RoleEntity>(b =>
        {
            b.ToTable("roles", Schemas.Identity);
            b.Property(r => r.Id).ValueGeneratedNever();
            b.HasIndex(r => r.NormalizedName).HasDatabaseName("ix_roles_normalized_name").IsUnique();
        });

        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles", Schemas.Identity);
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims", Schemas.Identity);
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins", Schemas.Identity);
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens", Schemas.Identity);
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims", Schemas.Identity);

        return builder;
    }
}
