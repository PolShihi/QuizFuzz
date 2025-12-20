using System.Text;
using System.Text.RegularExpressions;

namespace QuizFuzz.Infrastructure.Services.FuzzyMatching;

/// <summary>
/// Нормализатор текста для нечеткого сопоставления
/// </summary>
public static class TextNormalizer
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex PunctuationRegex = new(@"[^\w\s]", RegexOptions.Compiled);
    
    /// <summary>
    /// Базовая нормализация текста
    /// </summary>
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // 1. Lowercase
        var normalized = text.ToLowerInvariant();

        // 2. Remove punctuation
        normalized = PunctuationRegex.Replace(normalized, " ");

        // 3. Normalize whitespace
        normalized = WhitespaceRegex.Replace(normalized, " ");

        // 4. Trim
        normalized = normalized.Trim();

        return normalized;
    }

    /// <summary>
    /// Агрессивная нормализация (удаление всех пробелов)
    /// </summary>
    public static string NormalizeAggressive(string text)
    {
        var normalized = Normalize(text);
        return normalized.Replace(" ", "");
    }

    /// <summary>
    /// Разбиение на токены
    /// </summary>
    public static List<string> Tokenize(string text)
    {
        var normalized = Normalize(text);
        return normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    /// <summary>
    /// Сортировка токенов (для token-sort similarity)
    /// </summary>
    public static string SortTokens(string text)
    {
        var tokens = Tokenize(text);
        tokens.Sort();
        return string.Join(" ", tokens);
    }

    /// <summary>
    /// Удаление дубликатов токенов (для token-set similarity)
    /// </summary>
    public static string UniqueTokens(string text)
    {
        var tokens = Tokenize(text);
        var unique = tokens.Distinct().OrderBy(t => t);
        return string.Join(" ", unique);
    }

    /// <summary>
    /// Транслитерация кириллицы в латиницу (базовая)
    /// </summary>
    public static string Transliterate(string text)
    {
        var translitMap = new Dictionary<char, string>
        {
            {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"},
            {'е', "e"}, {'ё', "yo"}, {'ж', "zh"}, {'з', "z"}, {'и', "i"},
            {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"}, {'н', "n"},
            {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"},
            {'у', "u"}, {'ф', "f"}, {'х', "h"}, {'ц', "ts"}, {'ч', "ch"},
            {'ш', "sh"}, {'щ', "sch"}, {'ъ', ""}, {'ы', "y"}, {'ь', ""},
            {'э', "e"}, {'ю', "yu"}, {'я', "ya"}
        };

        var sb = new StringBuilder();
        foreach (var c in text.ToLowerInvariant())
        {
            if (translitMap.TryGetValue(c, out var replacement))
                sb.Append(replacement);
            else
                sb.Append(c);
        }

        return sb.ToString();
    }
}
