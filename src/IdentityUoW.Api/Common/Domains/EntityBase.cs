namespace IdentityUoW.Api.Common.Domains;

public interface IEntityBase<TKey>
{
    TKey Id { get; set; }
}

public abstract class EntityBase<TKey> : IEntityBase<TKey>
{
    public TKey Id { get; set; } = default!;
}
