using QuizFuzz.Domain.Common;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Канонический ответ на вопрос
/// </summary>
public class QuestionAnswer : BaseEntity
{
    public Guid QuestionId { get; private set; }
    public string AnswerText { get; private set; }
    public string NormalizedAnswer { get; private set; }
    public bool IsPrimary { get; private set; }
    public string LanguageCode { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties
    public Question Question { get; private set; } = null!;

    private readonly List<FuzzyAlias> _aliases = new();
    public IReadOnlyCollection<FuzzyAlias> Aliases => _aliases.AsReadOnly();

    private QuestionAnswer() { } // EF Core

    public QuestionAnswer(
        Guid questionId,
        string answerText,
        bool isPrimary = false,
        string languageCode = "ru")
    {
        if (string.IsNullOrWhiteSpace(answerText))
            throw new ArgumentException("Answer text cannot be empty", nameof(answerText));

        QuestionId = questionId;
        AnswerText = answerText.Trim();
        NormalizedAnswer = NormalizeText(answerText);
        IsPrimary = isPrimary;
        LanguageCode = languageCode;
        IsActive = true;
    }

    public void UpdateAnswerText(string answerText)
    {
        if (string.IsNullOrWhiteSpace(answerText))
            throw new ArgumentException("Answer text cannot be empty", nameof(answerText));

        AnswerText = answerText.Trim();
        NormalizedAnswer = NormalizeText(answerText);
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void SetAsAlternative()
    {
        IsPrimary = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public FuzzyAlias AddAlias(string aliasText, AliasKind kind)
    {
        if (string.IsNullOrWhiteSpace(aliasText))
            throw new ArgumentException("Alias text cannot be empty", nameof(aliasText));

        var alias = new FuzzyAlias(Id, aliasText, kind);
        
        // Check for duplicate normalized alias
        if (_aliases.Any(a => a.NormalizedAlias.Equals(alias.NormalizedAlias, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("This alias already exists");

        _aliases.Add(alias);
        return alias;
    }

    public void RemoveAlias(Guid aliasId)
    {
        var alias = _aliases.FirstOrDefault(a => a.Id == aliasId);
        if (alias != null)
        {
            _aliases.Remove(alias);
        }
    }

    private static string NormalizeText(string text)
    {
        // Basic normalization
        var result = text.Trim().ToLowerInvariant();
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
        return result;
    }
}
