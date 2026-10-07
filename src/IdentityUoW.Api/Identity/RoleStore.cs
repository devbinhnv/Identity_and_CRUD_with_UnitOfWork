using IdentityUoW.Api.Entities;
using IdentityUoW.Api.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityUoW.Api.Identity;

/// <inheritdoc cref="UserStore"/>
public class RoleStore : RoleStore<RoleEntity, AppDbContext, Guid>
{
    public RoleStore(AppDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        AutoSaveChanges = false;
    }

    public override Task<IdentityResult> UpdateAsync(RoleEntity role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (Context.Entry(role).State != EntityState.Added)
        {
            return base.UpdateAsync(role, cancellationToken);
        }

        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        return Task.FromResult(IdentityResult.Success);
    }
}
