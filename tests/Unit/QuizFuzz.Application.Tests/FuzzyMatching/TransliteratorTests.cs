using QuizFuzz.Infrastructure.Services.FuzzyMatching;
using Xunit;

namespace QuizFuzz.Application.Tests.FuzzyMatching;

public class TransliteratorTests
{
    #region ToLatin Tests
    
    [Fact]
    public void ToLatin_SimpleRussianWord_ConvertsCorrectly()
    {
        // Arrange
        var input = "Москва";
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal("Moskva", result);
    }
    
    [Fact]
    public void ToLatin_WordWithYo_ConvertsToYo()
    {
        // Arrange
        var input = "Ёлка";
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal("Yolka", result);
    }
    
    [Fact]
    public void ToLatin_WordWithShch_ConvertsToShch()
    {
        // Arrange
        var input = "Щука";
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal("Shchuka", result);
    }
    
    [Fact]
    public void ToLatin_WordWithSoftAndHardSign_RemovesSigns()
    {
        // Arrange
        var input = "Объявление";
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal("Obyavlenie", result);
    }
    
    [Theory]
    [InlineData("Париж", "Parizh")]
    [InlineData("Толстой", "Tolstoy")] // й → y (GOST 7.79-2000)
    [InlineData("Достоевский", "Dostoevskiy")] // й → y (GOST 7.79-2000)
    [InlineData("Санкт-Петербург", "Sankt-Peterburg")]
    public void ToLatin_RussianNames_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void ToLatin_Tchaikovsky_ConvertsWithCh()
    {
        // Arrange
        var input = "Чайковский";
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert - should start with Ch (Ч -> Ch in GOST)
        Assert.StartsWith("Ch", result);
        Assert.Contains("kovskiy", result); // й → y (GOST 7.79-2000)
    }
    
    [Theory]
    [InlineData("Россия", "Rossiya")] // я → ya
    [InlineData("Украина", "Ukraina")]
    [InlineData("Беларусь", "Belarus")] // ь → "" (удаляется)
    [InlineData("Казахстан", "Kazakhstan")]
    public void ToLatin_Countries_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void ToLatin_AlreadyLatinText_RemainsUnchanged()
    {
        // Arrange
        var input = "Moscow";
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal("Moscow", result);
    }
    
    [Fact]
    public void ToLatin_MixedCyrillicAndLatin_ConvertsOnlyCyrillic()
    {
        // Arrange
        var input = "Москва-Moscow";
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal("Moskva-Moscow", result);
    }
    
    [Fact]
    public void ToLatin_WithNumbers_KeepsNumbers()
    {
        // Arrange
        var input = "Москва 1939";
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal("Moskva 1939", result);
    }
    
    [Fact]
    public void ToLatin_WithPunctuation_KeepsPunctuation()
    {
        // Arrange
        var input = "Москва, столица России!";
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal("Moskva, stolitsa Rossii!", result);
    }
    
    [Fact]
    public void ToLatin_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var input = "";
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal("", result);
    }
    
    [Fact]
    public void ToLatin_Null_ReturnsNull()
    {
        // Arrange
        string? input = null;
        
        // Act
        var result = Transliterator.ToLatin(input!);
        
        // Assert
        Assert.Null(result);
    }
    
    #endregion
    
    #region HasCyrillic Tests
    
    [Theory]
    [InlineData("Москва", true)]
    [InlineData("Париж", true)]
    [InlineData("Moscow", false)]
    [InlineData("Paris", false)]
    [InlineData("Москва123", true)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void HasCyrillic_VariousInputs_DetectsCorrectly(string input, bool expected)
    {
        // Act
        var result = Transliterator.HasCyrillic(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void HasCyrillic_MixedText_ReturnsTrue()
    {
        // Arrange
        var input = "Moscow-Москва";
        
        // Act
        var result = Transliterator.HasCyrillic(input);
        
        // Assert
        Assert.True(result);
    }
    
    #endregion
    
    #region HasLatin Tests
    
    [Theory]
    [InlineData("Moscow", true)]
    [InlineData("Paris", true)]
    [InlineData("Москва", false)]
    [InlineData("Париж", false)]
    [InlineData("Moscow123", true)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void HasLatin_VariousInputs_DetectsCorrectly(string input, bool expected)
    {
        // Act
        var result = Transliterator.HasLatin(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void HasLatin_MixedText_ReturnsTrue()
    {
        // Arrange
        var input = "Москва-Moscow";
        
        // Act
        var result = Transliterator.HasLatin(input);
        
        // Assert
        Assert.True(result);
    }
    
    #endregion
    
    #region Edge Cases
    
    [Fact]
    public void ToLatin_AllCyrillicLetters_ConvertsAll()
    {
        // Arrange - все буквы кириллицы (а-я)
        var input = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
        // GOST 7.79-2000: й→y, ю→yu, я→ya, ъ→"", ь→""
        var expected = "abvgdeyozhziyklmnoprstufkhtschshshchyeyuya";
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void ToLatin_AllCyrillicLettersUppercase_ConvertsAll()
    {
        // Arrange - все заглавные буквы (А-Я)
        var input = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
        // GOST 7.79-2000: Й→Y, Ю→Yu, Я→Ya, Ъ→"", Ь→""
        var expected = "ABVGDEYoZhZIYKLMNOPRSTUFKhTsChShShchYEYuYa";
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void ToLatin_VeryLongText_HandlesCorrectly()
    {
        // Arrange
        var input = string.Join(" ", Enumerable.Repeat("Москва", 1000));
        
        // Act
        var result = Transliterator.ToLatin(input);
        
        // Assert
        Assert.Contains("Moskva", result);
        // Just check it's not empty and contains expected text
        Assert.True(result.Length > 1000);
    }
    
    #endregion
}
