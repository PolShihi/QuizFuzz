using QuizFuzz.Application.Common.Models;

namespace QuizFuzz.Application.Auth.Commands.Register;

/// <summary>
/// Команда регистрации пользователя
/// </summary>
public record RegisterCommand
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Результат регистрации
/// </summary>
public record RegisterResult
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}
