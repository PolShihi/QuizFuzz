namespace QuizFuzz.Application.Common.Interfaces.Services;

/// <summary>
/// Сервис для работы с JWT токенами
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string username, string email, IEnumerable<string> roles);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    bool ValidateToken(string token);
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
