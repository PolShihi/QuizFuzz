namespace QuizFuzz.Application.Common.Interfaces.Services;

/// <summary>
/// Сервис для хеширования паролей
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
