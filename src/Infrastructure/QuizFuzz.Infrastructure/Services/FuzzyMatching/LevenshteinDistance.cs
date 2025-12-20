namespace QuizFuzz.Infrastructure.Services.FuzzyMatching;

/// <summary>
/// Алгоритм Левенштейна для вычисления расстояния редактирования
/// </summary>
public static class LevenshteinDistance
{
    /// <summary>
    /// Вычисление расстояния Левенштейна между двумя строками
    /// </summary>
    public static int Calculate(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return target?.Length ?? 0;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;

        // Оптимизация: используем только две строки матрицы
        var previousRow = new int[targetLength + 1];
        var currentRow = new int[targetLength + 1];

        // Инициализация первой строки
        for (var i = 0; i <= targetLength; i++)
            previousRow[i] = i;

        for (var i = 1; i <= sourceLength; i++)
        {
            currentRow[0] = i;

            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                currentRow[j] = Math.Min(
                    Math.Min(
                        currentRow[j - 1] + 1,      // Insertion
                        previousRow[j] + 1),        // Deletion
                    previousRow[j - 1] + cost);     // Substitution
            }

            // Swap rows
            var temp = previousRow;
            previousRow = currentRow;
            currentRow = temp;
        }

        return previousRow[targetLength];
    }

    /// <summary>
    /// Вычисление similarity (0.0 - 1.0) на основе расстояния Левенштейна
    /// </summary>
    public static decimal CalculateSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
            return 1.0m;

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0m;

        var distance = Calculate(source, target);
        var maxLength = Math.Max(source.Length, target.Length);

        return 1.0m - (decimal)distance / maxLength;
    }

    /// <summary>
    /// Проверка, находится ли расстояние в пределах threshold
    /// </summary>
    public static bool IsWithinThreshold(string source, string target, int threshold)
    {
        var distance = Calculate(source, target);
        return distance <= threshold;
    }

    /// <summary>
    /// Вычисление расстояния Дамерау-Левенштейна (с транспозицией)
    /// </summary>
    public static int CalculateDamerau(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return target?.Length ?? 0;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;
        var matrix = new int[sourceLength + 1, targetLength + 1];

        for (var i = 0; i <= sourceLength; i++)
            matrix[i, 0] = i;

        for (var j = 0; j <= targetLength; j++)
            matrix[0, j] = j;

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(
                        matrix[i - 1, j] + 1,       // Deletion
                        matrix[i, j - 1] + 1),      // Insertion
                    matrix[i - 1, j - 1] + cost);   // Substitution

                // Transposition
                if (i > 1 && j > 1 &&
                    source[i - 1] == target[j - 2] &&
                    source[i - 2] == target[j - 1])
                {
                    matrix[i, j] = Math.Min(
                        matrix[i, j],
                        matrix[i - 2, j - 2] + cost);
                }
            }
        }

        return matrix[sourceLength, targetLength];
    }
}
