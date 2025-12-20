namespace QuizFuzz.Application.Common.Interfaces.Services;

/// <summary>
/// Сервис для получения информации о текущем пользователе
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    IEnumerable<string> GetRoles();
}
