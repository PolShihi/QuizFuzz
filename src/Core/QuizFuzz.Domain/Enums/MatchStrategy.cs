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
    Transliteration = 5, //  НОВОЕ: Транслитерация + Levenshtein
    Phonetic = 6,        //  ОБНОВЛЕНО: Фонетическое (Soundex + Metaphone)
    Semantic = 7,
    Rejected = 8
}
