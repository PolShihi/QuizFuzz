using System.Text;

namespace QuizFuzz.Infrastructure.Services.FuzzyMatching;

/// <summary>
/// Soundex - фонетический алгоритм для английского языка.
/// Кодирует слова по их звучанию в 4-символьный код.
/// </summary>
public static class SoundexAlgorithm
{
    private static readonly Dictionary<char, char> SoundexMapping = new()
    {
        {'B', '1'}, {'F', '1'}, {'P', '1'}, {'V', '1'},
        {'C', '2'}, {'G', '2'}, {'J', '2'}, {'K', '2'}, {'Q', '2'}, {'S', '2'}, {'X', '2'}, {'Z', '2'},
        {'D', '3'}, {'T', '3'},
        {'L', '4'},
        {'M', '5'}, {'N', '5'},
        {'R', '6'}
    };
    
    /// <summary>
    /// Генерирует Soundex код для слова
    /// </summary>
    public static string Encode(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return "0000";
            
        word = word.ToUpperInvariant();
        
        // Удаляем все кроме букв
        word = new string(word.Where(char.IsLetter).ToArray());
        
        if (word.Length == 0)
            return "0000";
            
        var result = new StringBuilder();
        
        // Первая буква остаётся как есть
        result.Append(word[0]);
        
        char prevCode = GetSoundexCode(word[0]);
        
        for (int i = 1; i < word.Length && result.Length < 4; i++)
        {
            char code = GetSoundexCode(word[i]);
            
            // Игнорируем:
            // - гласные (0)
            // - повторяющиеся коды
            if (code != '0' && code != prevCode)
            {
                result.Append(code);
            }
            
            // Обновляем prevCode только для не-нулевых кодов
            if (code != '0')
            {
                prevCode = code;
            }
        }
        
        // Дополняем нулями до 4 символов
        while (result.Length < 4)
        {
            result.Append('0');
        }
        
        return result.ToString();
    }
    
    private static char GetSoundexCode(char c)
    {
        if (SoundexMapping.TryGetValue(c, out var code))
            return code;
            
        // Гласные и H, W, Y игнорируются (код 0)
        return '0';
    }
    
    /// <summary>
    /// Сравнивает два слова по их Soundex кодам
    /// </summary>
    public static bool AreSimilar(string word1, string word2)
    {
        return Encode(word1) == Encode(word2);
    }
}
