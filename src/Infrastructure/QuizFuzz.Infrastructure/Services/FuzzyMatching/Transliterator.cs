using System.Text;

namespace QuizFuzz.Infrastructure.Services.FuzzyMatching;

/// <summary>
/// Транслитерация текста между русским и латинским алфавитами.
/// Использует стандарт GOST 7.79-2000 (System B)
/// </summary>
public static class Transliterator
{
    // GOST 7.79-2000 (System B) - наиболее распространённый стандарт
    private static readonly Dictionary<char, string> RuToEn = new()
    {
        // Строчные
        {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"},
        {'е', "e"}, {'ё', "yo"}, {'ж', "zh"}, {'з', "z"}, {'и', "i"},
        {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"}, {'н', "n"},
        {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"},
        {'у', "u"}, {'ф', "f"}, {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"},
        {'ш', "sh"}, {'щ', "shch"}, {'ъ', ""}, {'ы', "y"}, {'ь', ""},
        {'э', "e"}, {'ю', "yu"}, {'я', "ya"},
        
        // Прописные
        {'А', "A"}, {'Б', "B"}, {'В', "V"}, {'Г', "G"}, {'Д', "D"},
        {'Е', "E"}, {'Ё', "Yo"}, {'Ж', "Zh"}, {'З', "Z"}, {'И', "I"},
        {'Й', "Y"}, {'К', "K"}, {'Л', "L"}, {'М', "M"}, {'Н', "N"},
        {'О', "O"}, {'П', "P"}, {'Р', "R"}, {'С', "S"}, {'Т', "T"},
        {'У', "U"}, {'Ф', "F"}, {'Х', "Kh"}, {'Ц', "Ts"}, {'Ч', "Ch"},
        {'Ш', "Sh"}, {'Щ', "Shch"}, {'Ъ', ""}, {'Ы', "Y"}, {'Ь', ""},
        {'Э', "E"}, {'Ю', "Yu"}, {'Я', "Ya"}
    };
    
    /// <summary>
    /// Конвертирует русский текст в латиницу
    /// </summary>
    public static string ToLatin(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        var sb = new StringBuilder(text.Length * 2);
        
        foreach (var c in text)
        {
            if (RuToEn.TryGetValue(c, out var latin))
            {
                sb.Append(latin);
            }
            else
            {
                // Оставляем как есть (латиница, цифры, знаки)
                sb.Append(c);
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Проверяет, содержит ли текст кириллические символы
    /// </summary>
    public static bool HasCyrillic(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
            
        return text.Any(c => (c >= 'А' && c <= 'я') || c == 'Ё' || c == 'ё');
    }
    
    /// <summary>
    /// Проверяет, содержит ли текст латинские символы
    /// </summary>
    public static bool HasLatin(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
            
        return text.Any(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));
    }
}
