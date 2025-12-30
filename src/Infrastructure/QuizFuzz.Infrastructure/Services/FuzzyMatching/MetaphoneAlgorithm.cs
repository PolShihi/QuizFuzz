using System.Text;

namespace QuizFuzz.Infrastructure.Services.FuzzyMatching;

/// <summary>
/// Metaphone - улучшенный фонетический алгоритм для английского языка.
/// Более точный чем Soundex, лучше обрабатывает сложные комбинации букв.
/// </summary>
public static class MetaphoneAlgorithm
{
    private const int MaxLength = 4;
    
    /// <summary>
    /// Генерирует Metaphone код для слова
    /// </summary>
    public static string Encode(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return string.Empty;
            
        word = word.ToUpperInvariant();
        
        // Удаляем все кроме букв
        word = new string(word.Where(char.IsLetter).ToArray());
        
        if (word.Length == 0)
            return string.Empty;
            
        var result = new StringBuilder();
        int i = 0;
        
        // Специальная обработка начальных комбинаций
        if (word.StartsWith("PN") || word.StartsWith("KN") || 
            word.StartsWith("GN") || word.StartsWith("AE") || 
            word.StartsWith("WR"))
        {
            i = 1; // Пропускаем первую букву
        }
        else if (word.StartsWith("WH"))
        {
            result.Append('W');
            i = 2;
        }
        else if (word.StartsWith("X"))
        {
            result.Append('S');
            i = 1;
        }
        
        while (i < word.Length && result.Length < MaxLength)
        {
            char current = word[i];
            char? next = i + 1 < word.Length ? word[i + 1] : null;
            char? prev = i > 0 ? word[i - 1] : null;
            
            switch (current)
            {
                case 'A': case 'E': case 'I': case 'O': case 'U':
                    if (i == 0) result.Append(current);
                    break;
                    
                case 'B':
                    if (!(prev == 'M' && next == null))
                        result.Append('B');
                    break;
                    
                case 'C':
                    if (next == 'H')
                    {
                        result.Append('X');
                        i++;
                    }
                    else if (next == 'I' && i + 2 < word.Length && word[i + 2] == 'A')
                    {
                        result.Append('X');
                    }
                    else if (next == 'E' || next == 'I' || next == 'Y')
                    {
                        result.Append('S');
                    }
                    else
                    {
                        result.Append('K');
                    }
                    break;
                    
                case 'D':
                    if (next == 'G' && i + 2 < word.Length && 
                        (word[i + 2] == 'E' || word[i + 2] == 'I' || word[i + 2] == 'Y'))
                    {
                        result.Append('J');
                        i++;
                    }
                    else
                    {
                        result.Append('T');
                    }
                    break;
                    
                case 'G':
                    if (next == 'H' && i + 2 < word.Length && !IsVowel(word[i + 2]))
                    {
                        // GH не произносится в конце или перед согласной
                    }
                    else if (next == 'N' && i + 1 == word.Length - 1)
                    {
                        // GN в конце слова
                    }
                    else if (next == 'E' || next == 'I' || next == 'Y')
                    {
                        result.Append('J');
                    }
                    else
                    {
                        result.Append('K');
                    }
                    break;
                    
                case 'H':
                    if (i == 0 || IsVowel(prev.Value))
                    {
                        if (next.HasValue && IsVowel(next.Value))
                            result.Append('H');
                    }
                    break;
                    
                case 'F': case 'J': case 'L': case 'M': case 'N': case 'R':
                    result.Append(current);
                    break;
                    
                case 'K':
                    if (prev != 'C')
                        result.Append('K');
                    break;
                    
                case 'P':
                    if (next == 'H')
                    {
                        result.Append('F');
                        i++;
                    }
                    else
                    {
                        result.Append('P');
                    }
                    break;
                    
                case 'Q':
                    result.Append('K');
                    break;
                    
                case 'S':
                    if (next == 'H')
                    {
                        result.Append('X');
                        i++;
                    }
                    else if (next == 'I' && i + 2 < word.Length && 
                             (word[i + 2] == 'O' || word[i + 2] == 'A'))
                    {
                        result.Append('X');
                    }
                    else
                    {
                        result.Append('S');
                    }
                    break;
                    
                case 'T':
                    if (next == 'H')
                    {
                        result.Append('0'); // TH звук
                        i++;
                    }
                    else if (next == 'I' && i + 2 < word.Length && 
                             (word[i + 2] == 'O' || word[i + 2] == 'A'))
                    {
                        result.Append('X');
                    }
                    else
                    {
                        result.Append('T');
                    }
                    break;
                    
                case 'V':
                    result.Append('F');
                    break;
                    
                case 'W': case 'Y':
                    if (next.HasValue && IsVowel(next.Value))
                        result.Append(current);
                    break;
                    
                case 'X':
                    result.Append("KS");
                    break;
                    
                case 'Z':
                    result.Append('S');
                    break;
            }
            
            i++;
        }
        
        return result.ToString();
    }
    
    private static bool IsVowel(char c)
    {
        return c == 'A' || c == 'E' || c == 'I' || c == 'O' || c == 'U';
    }
    
    /// <summary>
    /// Сравнивает два слова по их Metaphone кодам
    /// </summary>
    public static bool AreSimilar(string word1, string word2)
    {
        return Encode(word1) == Encode(word2);
    }
    
    /// <summary>
    /// Вычисляет сходство между двумя словами на основе Metaphone (0.0 - 1.0)
    /// </summary>
    public static decimal CalculateSimilarity(string word1, string word2)
    {
        var code1 = Encode(word1);
        var code2 = Encode(word2);
        
        if (string.IsNullOrEmpty(code1) || string.IsNullOrEmpty(code2))
            return 0m;
            
        if (code1 == code2)
            return 1.0m;
            
        // Частичное совпадение
        var maxLen = Math.Max(code1.Length, code2.Length);
        var matching = 0;
        
        for (int i = 0; i < Math.Min(code1.Length, code2.Length); i++)
        {
            if (code1[i] == code2[i])
                matching++;
        }
        
        return (decimal)matching / maxLen;
    }
}
