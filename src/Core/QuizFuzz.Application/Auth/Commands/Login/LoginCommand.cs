namespace QuizFuzz.Application.Auth.Commands.Login;

/// <summary>
/// Команда входа пользователя
/// </summary>
public record LoginCommand
{
    public string EmailOrUsername { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Результат входа
/// </summary>
public record LoginResult
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public IEnumerable<string> Roles { get; init; } = Enumerable.Empty<string>();
}
