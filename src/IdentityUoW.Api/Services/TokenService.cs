using System.Security.Claims;
using System.Text;
using IdentityUoW.Api.Configurations;
using IdentityUoW.Api.Dtos;
using IdentityUoW.Api.Entities;
using IdentityUoW.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityUoW.Api.Services;

public class TokenService(IOptions<JwtSettings> options) : ITokenService
{
    private readonly JsonWebTokenHandler _handler = new();

    public TokenDto GenerateToken(UserEntity user, IEnumerable<string> roles)
    {
        var settings = options.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(settings.ExpiresInMinutes);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            .. roles.Select(role => new Claim(ClaimTypes.Role, role))
        ];

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
                SecurityAlgorithms.HmacSha256)
        });

        return new TokenDto(token, expiresAt);
    }
}
