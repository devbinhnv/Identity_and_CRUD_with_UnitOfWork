using IdentityUoW.Api.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityUoW.Api.Identity;

/// <summary>
/// Identity keeps all its behaviour (validation, normalization, hashing, security stamp)
/// but no longer calls SaveChanges: <c>IdentityResult.Succeeded</c> means "tracked", not "committed".
/// The Unit of Work commits.
/// </summary>
public class UserStore : UserStore<UserEntity, RoleEntity, AppDbContext, Guid>
{
    public UserStore(AppDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        AutoSaveChanges = false;
    }

    /// <summary>
    /// A user can be created and then updated (e.g. AddToRoleAsync) in the same Unit of Work.
    /// The base implementation calls Attach/Update, which would turn the pending INSERT into an
    /// UPDATE of a row that does not exist yet. A still-Added user only needs a new stamp.
    /// </summary>
    public override Task<IdentityResult> UpdateAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (Context.Entry(user).State != EntityState.Added)
        {
            return base.UpdateAsync(user, cancellationToken);
        }

        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        return Task.FromResult(IdentityResult.Success);
    }
}
