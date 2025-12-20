namespace QuizFuzz.Application.Common.Interfaces.Services;

/// <summary>
/// Провайдер для работы с датой и временем (для тестируемости)
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateTimeOffset UtcNowOffset { get; }
}
