using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.ValueObjects;

/// <summary>
/// Value Object для нормализованного текста
/// </summary>
public sealed class NormalizedText : ValueObject
{
    public string Value { get; private set; }
    public string Original { get; private set; }

    private NormalizedText(string original, string normalized)
    {
        Original = original;
        Value = normalized;
    }

    public static NormalizedText Create(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or whitespace", nameof(text));

        var normalized = Normalize(text);
        return new NormalizedText(text, normalized);
    }

    private static string Normalize(string text)
    {
        // Базовая нормализация: trim, lowercase, удаление лишних пробелов
        var result = text.Trim().ToLowerInvariant();
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
        return result;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
