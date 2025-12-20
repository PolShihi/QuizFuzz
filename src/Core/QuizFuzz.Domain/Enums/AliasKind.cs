namespace QuizFuzz.Domain.Enums;

/// <summary>
/// Тип алиаса для нечеткого сопоставления
/// </summary>
public enum AliasKind
{
    Synonym = 1,
    AltSpelling = 2,
    Translit = 3,
    CommonMisspell = 4
}
