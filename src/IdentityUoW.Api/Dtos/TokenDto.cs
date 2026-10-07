namespace IdentityUoW.Api.Dtos;

public record TokenDto(string AccessToken, DateTime ExpiresAt, string TokenType = "Bearer");
