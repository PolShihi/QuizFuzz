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
    
    //  НОВОЕ: Настройки fuzzy matching для этого ответа
    /// <summary>
    /// Разрешено ли использовать fuzzy matching для этого ответа
    /// </summary>
    public bool AllowFuzzyMatch { get; private set; }
    
    /// <summary>
    /// Максимальное расстояние Левенштейна (EditDistance) для считывания ответа правильным.
    /// Если null - используется значение по умолчанию (2).
    /// Для точных ответов (числа, даты) должно быть 0 или 1.
    /// </summary>
    public int? MaxEditDistance { get; private set; }
    
    /// <summary>
    /// Индивидуальный порог принятия ответа (0.5 - 1.0).
    /// Ответ считается правильным только если фактическая уверенность fuzzy-сравнения
    /// больше либо равна этому порогу. Если null - используется значение по умолчанию.
    /// </summary>
    public decimal? MinConfidence { get; private set; }

    /// <summary>
    /// Алиас для бизнес-смысла поля MinConfidence: порог принятия ответа.
    /// Оставляем MinConfidence для совместимости с существующей БД и DTO.
    /// </summary>
    public decimal? AcceptanceThreshold => MinConfidence;

    // Navigation properties
    public Question Question { get; private set; } = null!;

    private readonly List<FuzzyAlias> _aliases = new();
    public IReadOnlyCollection<FuzzyAlias> Aliases => _aliases.AsReadOnly();

    private QuestionAnswer() { } // EF Core

    public QuestionAnswer(
        Guid questionId,
        string answerText,
        bool isPrimary = false,
        string languageCode = "ru",
        bool allowFuzzyMatch = true,
        int? maxEditDistance = null,
        decimal? minConfidence = null)
    {
        if (string.IsNullOrWhiteSpace(answerText))
            throw new ArgumentException("Answer text cannot be empty", nameof(answerText));

        // Валидация fuzzy matching настроек
        if (maxEditDistance.HasValue && maxEditDistance.Value < 0)
            throw new ArgumentException("MaxEditDistance cannot be negative", nameof(maxEditDistance));
            
        if (minConfidence.HasValue && (minConfidence.Value < 0.5m || minConfidence.Value > 1))
            throw new ArgumentException("MinConfidence must be between 0.5 and 1", nameof(minConfidence));

        QuestionId = questionId;
        AnswerText = answerText.Trim();
        NormalizedAnswer = NormalizeText(answerText);
        IsPrimary = isPrimary;
        LanguageCode = languageCode;
        IsActive = true;
        
        // Fuzzy matching настройки
        AllowFuzzyMatch = allowFuzzyMatch;
        MaxEditDistance = maxEditDistance;
        MinConfidence = minConfidence;
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
    
    /// <summary>
    /// Обновить настройки fuzzy matching для этого ответа, включая индивидуальный порог принятия.
    /// </summary>
    public void UpdateFuzzyMatchSettings(
        bool allowFuzzyMatch,
        int? maxEditDistance = null,
        decimal? minConfidence = null)
    {
        // Валидация
        if (maxEditDistance.HasValue && maxEditDistance.Value < 0)
            throw new ArgumentException("MaxEditDistance cannot be negative", nameof(maxEditDistance));
            
        if (minConfidence.HasValue && (minConfidence.Value < 0.5m || minConfidence.Value > 1))
            throw new ArgumentException("MinConfidence must be between 0.5 and 1", nameof(minConfidence));
        
        AllowFuzzyMatch = allowFuzzyMatch;
        MaxEditDistance = maxEditDistance;
        MinConfidence = minConfidence;
    }
    
    /// <summary>
    /// Установить строгое сравнение (для чисел, дат, etc)
    /// </summary>
    public void SetStrictMatching()
    {
        AllowFuzzyMatch = false;
        MaxEditDistance = 0;
        MinConfidence = 1.0m;
    }
    
    /// <summary>
    /// Установить мягкое сравнение (для текстовых ответов)
    /// </summary>
    public void SetFlexibleMatching(int maxEditDistance = 2, decimal minConfidence = 0.75m)
    {
        if (maxEditDistance < 0)
            throw new ArgumentException("MaxEditDistance cannot be negative", nameof(maxEditDistance));
            
        if (minConfidence < 0.5m || minConfidence > 1)
            throw new ArgumentException("MinConfidence must be between 0.5 and 1", nameof(minConfidence));
        
        AllowFuzzyMatch = true;
        MaxEditDistance = maxEditDistance;
        MinConfidence = minConfidence;
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
