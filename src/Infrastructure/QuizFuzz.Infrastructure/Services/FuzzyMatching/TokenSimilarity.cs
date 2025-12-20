namespace QuizFuzz.Infrastructure.Services.FuzzyMatching;

/// <summary>
/// Вычисление сходства на основе токенов
/// </summary>
public static class TokenSimilarity
{
    /// <summary>
    /// Token Sort Ratio - сравнение отсортированных токенов
    /// </summary>
    public static decimal CalculateSortRatio(string source, string target)
    {
        var sortedSource = TextNormalizer.SortTokens(source);
        var sortedTarget = TextNormalizer.SortTokens(target);

        return LevenshteinDistance.CalculateSimilarity(sortedSource, sortedTarget);
    }

    /// <summary>
    /// Token Set Ratio - сравнение уникальных токенов
    /// </summary>
    public static decimal CalculateSetRatio(string source, string target)
    {
        var sourceTokens = new HashSet<string>(TextNormalizer.Tokenize(source));
        var targetTokens = new HashSet<string>(TextNormalizer.Tokenize(target));

        if (sourceTokens.Count == 0 && targetTokens.Count == 0)
            return 1.0m;

        if (sourceTokens.Count == 0 || targetTokens.Count == 0)
            return 0.0m;

        var intersection = sourceTokens.Intersect(targetTokens).Count();
        var union = sourceTokens.Union(targetTokens).Count();

        return (decimal)intersection / union;
    }

    /// <summary>
    /// Partial Ratio - поиск лучшего совпадения подстроки
    /// </summary>
    public static decimal CalculatePartialRatio(string source, string target)
    {
        var shorter = source.Length <= target.Length ? source : target;
        var longer = source.Length > target.Length ? source : target;

        if (shorter.Length == 0)
            return 0.0m;

        var bestSimilarity = 0.0m;

        // Sliding window
        for (var i = 0; i <= longer.Length - shorter.Length; i++)
        {
            var substring = longer.Substring(i, shorter.Length);
            var similarity = LevenshteinDistance.CalculateSimilarity(shorter, substring);

            if (similarity > bestSimilarity)
                bestSimilarity = similarity;

            if (bestSimilarity == 1.0m)
                break;
        }

        return bestSimilarity;
    }

    /// <summary>
    /// Weighted Ratio - комбинированная оценка с весами
    /// </summary>
    public static decimal CalculateWeightedRatio(string source, string target)
    {
        var basicSimilarity = LevenshteinDistance.CalculateSimilarity(source, target);
        var sortRatio = CalculateSortRatio(source, target);
        var setRatio = CalculateSetRatio(source, target);
        var partialRatio = CalculatePartialRatio(source, target);

        // Weighted average
        return (basicSimilarity * 0.3m +
                sortRatio * 0.3m +
                setRatio * 0.2m +
                partialRatio * 0.2m);
    }
}
