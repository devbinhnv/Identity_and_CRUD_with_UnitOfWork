namespace IdentityUoW.Api.Persistence;

/// <summary>One PostgreSQL schema per area, so boundaries stay visible in the database.</summary>
public static class Schemas
{
    public const string Identity = "identity";
    public const string Catalog = "catalog";
}
