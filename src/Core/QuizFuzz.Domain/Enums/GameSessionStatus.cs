namespace QuizFuzz.Domain.Enums;

/// <summary>
/// Статус игровой сессии
/// </summary>
public enum GameSessionStatus
{
    Pending = 1,
    Active = 2,
    Finished = 3,
    Aborted = 4
}
