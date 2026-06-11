using QuizFuzz.Shared.Dtos.Auth;

namespace QuizFuzz.Client.Services.Auth;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<bool> RefreshTokenAsync();
    Task<string?> GetTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<UserDto?> GetCurrentUserAsync();
}
