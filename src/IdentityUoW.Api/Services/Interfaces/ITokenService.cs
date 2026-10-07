using IdentityUoW.Api.Dtos;
using IdentityUoW.Api.Entities;

namespace IdentityUoW.Api.Services.Interfaces;

public interface ITokenService
{
    TokenDto GenerateToken(UserEntity user, IEnumerable<string> roles);
}
