using IdentityUoW.Api.Common.Domains;

namespace IdentityUoW.Api.Entities;

/// <summary>Example business entity for the CRUD part.</summary>
public class ProductEntity : EntityAuditBase<Guid>
{
    public string No { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Summary { get; set; }

    public decimal Price { get; set; }

    /// <summary>Logical reference to identity.users (no FK, no navigation).</summary>
    public Guid CreatedBy { get; set; }
}
