using QuizFuzz.Application.Common.Interfaces.Services;

namespace QuizFuzz.Infrastructure.Services;

/// <summary>
/// Провайдер для работы с датой и временем (для тестируемости)
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    
    public DateTime Now => DateTime.Now;
    
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
}
