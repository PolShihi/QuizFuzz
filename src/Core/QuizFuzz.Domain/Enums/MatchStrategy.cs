namespace QuizFuzz.Domain.Enums;

/// <summary>
/// Стратегия нечеткого сопоставления ответа
/// </summary>
public enum MatchStrategy
{
    Exact = 1,
    Alias = 2,
    EditDistance = 3,
    Token = 4,
    Phonetic = 5,
    Semantic = 6,
    Rejected = 7
}
