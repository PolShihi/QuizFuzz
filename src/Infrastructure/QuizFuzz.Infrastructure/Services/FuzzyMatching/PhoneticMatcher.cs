namespace QuizFuzz.Infrastructure.Services.FuzzyMatching;

/// <summary>
/// Фонетическое сравнение с использованием нескольких алгоритмов
/// </summary>
public static class PhoneticMatcher
{
    /// <summary>
    /// Вычисляет фонетическое сходство между двумя словами.
    /// Использует Soundex и Metaphone для максимальной точности.
    /// </summary>
    /// <returns>Confidence от 0.0 до 1.0</returns>
    public static decimal CalculateSimilarity(string word1, string word2)
    {
        if (string.IsNullOrWhiteSpace(word1) || string.IsNullOrWhiteSpace(word2))
            return 0m;
            
        // 1. Soundex comparison
        var soundexMatch = SoundexAlgorithm.AreSimilar(word1, word2);
        
        // 2. Metaphone comparison  
        var metaphoneSimilarity = MetaphoneAlgorithm.CalculateSimilarity(word1, word2);
        
        // Комбинируем результаты (Metaphone более точный, даем ему больший вес)
        if (soundexMatch && metaphoneSimilarity >= 0.75m)
        {
            return Math.Max(0.85m, metaphoneSimilarity); // Оба совпали - высокая уверенность
        }
        else if (soundexMatch)
        {
            return 0.70m; // Только Soundex
        }
        else if (metaphoneSimilarity >= 0.75m)
        {
            return metaphoneSimilarity; // Только Metaphone
        }
        else
        {
            return metaphoneSimilarity * 0.8m; // Частичное совпадение
        }
    }
    
    /// <summary>
    /// Проверяет фонетическое сходство с учетом порога
    /// </summary>
    public static bool IsPhoneticMatch(string word1, string word2, decimal minConfidence = 0.70m)
    {
        return CalculateSimilarity(word1, word2) >= minConfidence;
    }
}
