using QuizFuzz.Domain.Common;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Алиас для нечеткого сопоставления ответов
/// </summary>
public class FuzzyAlias : BaseEntity
{
    public Guid QuestionAnswerId { get; private set; }
    public string AliasText { get; private set; }
    public string NormalizedAlias { get; private set; }
    public AliasKind Kind { get; private set; }

    // Navigation properties
    public QuestionAnswer QuestionAnswer { get; private set; } = null!;

    private FuzzyAlias() { } // EF Core

    public FuzzyAlias(Guid questionAnswerId, string aliasText, AliasKind kind)
    {
        if (string.IsNullOrWhiteSpace(aliasText))
            throw new ArgumentException("Alias text cannot be empty", nameof(aliasText));

        QuestionAnswerId = questionAnswerId;
        AliasText = aliasText.Trim();
        NormalizedAlias = NormalizeText(aliasText);
        Kind = kind;
    }

    public void UpdateAliasText(string aliasText)
    {
        if (string.IsNullOrWhiteSpace(aliasText))
            throw new ArgumentException("Alias text cannot be empty", nameof(aliasText));

        AliasText = aliasText.Trim();
        NormalizedAlias = NormalizeText(aliasText);
    }

    public void UpdateKind(AliasKind kind)
    {
        Kind = kind;
    }

    private static string NormalizeText(string text)
    {
        var result = text.Trim().ToLowerInvariant();
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
        return result;
    }
}
