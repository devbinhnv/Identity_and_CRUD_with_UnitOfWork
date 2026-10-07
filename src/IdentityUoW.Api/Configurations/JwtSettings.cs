namespace IdentityUoW.Api.Configurations;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;

    /// <summary>HMAC-SHA256 key, at least 32 characters.</summary>
    public string SecretKey { get; set; } = null!;

    public int ExpiresInMinutes { get; set; } = 60;
}
