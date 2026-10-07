namespace IdentityUoW.Api.Common.Domains;

/// <summary>Filled automatically by <c>AppDbContext.SaveChangesAsync</c>.</summary>
public interface IDateTracking
{
    DateTimeOffset CreatedDate { get; set; }

    DateTimeOffset? LastModifiedDate { get; set; }
}

public abstract class EntityAuditBase<TKey> : EntityBase<TKey>, IDateTracking
{
    public DateTimeOffset CreatedDate { get; set; }

    public DateTimeOffset? LastModifiedDate { get; set; }
}
