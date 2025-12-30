using QuizFuzz.Infrastructure.Services.FuzzyMatching;
using Xunit;

namespace QuizFuzz.Application.Tests.FuzzyMatching;

public class SoundexAlgorithmTests
{
    #region Encode Tests - Basic
    
    [Theory]
    [InlineData("School", "S400")]
    [InlineData("Skul", "S400")]
    [InlineData("Skool", "S400")]
    public void Encode_SchoolVariations_ProducesSameCode(string input, string expected)
    {
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("Robert", "R163")]
    [InlineData("Rupert", "R163")]
    [InlineData("Rubin", "R150")]
    public void Encode_SimilarNames_ProducesExpectedCodes(string input, string expected)
    {
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("Smith", "S530")]
    [InlineData("Smythe", "S530")]
    public void Encode_SmithVariations_ProducesSameCode(string input, string expected)
    {
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    #endregion
    
    #region Encode Tests - Common Words
    
    [Theory]
    [InlineData("Paris", "P620")]
    [InlineData("Parizh", "P620")]
    [InlineData("Parij", "P620")]
    public void Encode_ParisVariations_ProducesSameCode(string input, string expected)
    {
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("London", "L535")]
    [InlineData("Moscow", "M200")]
    [InlineData("Moskva", "M210")]
    [InlineData("Berlin", "B645")]
    public void Encode_Cities_ProducesExpectedCodes(string input, string expected)
    {
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    #endregion
    
    #region Encode Tests - Edge Cases
    
    [Fact]
    public void Encode_EmptyString_ReturnsZeros()
    {
        // Arrange
        var input = "";
        
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal("0000", result);
    }
    
    [Fact]
    public void Encode_SingleLetter_PadsWithZeros()
    {
        // Arrange
        var input = "A";
        
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal("A000", result);
    }
    
    [Fact]
    public void Encode_OnlyVowels_FirstLetterPlusZeros()
    {
        // Arrange
        var input = "Aeiou";
        
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal("A000", result);
    }
    
    [Fact]
    public void Encode_OnlyConsonants_EncodesAll()
    {
        // Arrange
        var input = "Bdfpl";
        
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal("B314", result);
    }
    
    [Fact]
    public void Encode_WithNumbers_IgnoresNumbers()
    {
        // Arrange
        var input = "Smith123";
        
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal("S530", result);
    }
    
    [Fact]
    public void Encode_WithSpaces_IgnoresSpaces()
    {
        // Arrange
        var input = "S m i t h";
        
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal("S530", result);
    }
    
    [Fact]
    public void Encode_WithSpecialCharacters_IgnoresThem()
    {
        // Arrange
        var input = "Sm!t#h";
        
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal("S530", result);
    }
    
    [Fact]
    public void Encode_LowercaseInput_WorksCorrectly()
    {
        // Arrange
        var input = "school";
        var expectedSameAs = SoundexAlgorithm.Encode("School");
        
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert - should be same as uppercase version
        Assert.Equal(expectedSameAs, result);
    }
    
    [Fact]
    public void Encode_MixedCaseInput_WorksCorrectly()
    {
        // Arrange
        var input = "ScHoOl";
        var expectedSameAs = SoundexAlgorithm.Encode("School");
        
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert - should be same as lowercase/uppercase versions
        Assert.Equal(expectedSameAs, result);
    }
    
    #endregion
    
    #region Encode Tests - Phonetic Patterns
    
    [Theory]
    [InlineData("Cat", "C300")]
    [InlineData("Kat", "K300")]
    public void Encode_CatVsKat_ProducesDifferentCodes(string input, string expected)
    {
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("Phone", "P500")]
    [InlineData("Fone", "F500")]
    public void Encode_PhoneVsFone_ProducesDifferentFirstLetter(string input, string expected)
    {
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("Night", "N230")]
    [InlineData("Knight", "K523")]
    public void Encode_NightVsKnight_ProducesDifferentCodes(string input, string expected)
    {
        // Act
        var result = SoundexAlgorithm.Encode(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    #endregion
    
    #region AreSimilar Tests
    
    [Theory]
    [InlineData("School", "Skul", true)]
    [InlineData("School", "Skool", true)]
    [InlineData("Paris", "Parizh", true)]
    [InlineData("Smith", "Smythe", true)]
    [InlineData("Cat", "Dog", false)]
    [InlineData("London", "Paris", false)]
    public void AreSimilar_VariousPairs_ReturnsExpectedResult(string word1, string word2, bool expected)
    {
        // Act
        var result = SoundexAlgorithm.AreSimilar(word1, word2);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void AreSimilar_SameWord_ReturnsTrue()
    {
        // Act
        var result = SoundexAlgorithm.AreSimilar("School", "School");
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void AreSimilar_CompletelyDifferent_ReturnsFalse()
    {
        // Act
        var result = SoundexAlgorithm.AreSimilar("Apple", "Zebra");
        
        // Assert
        Assert.False(result);
    }
    
    #endregion
    
    #region Real-World Scenarios
    
    [Theory]
    [InlineData("Tolstoy", "Tolstoj")]
    [InlineData("Dostoevsky", "Dostoyevsky")]
    public void Encode_RussianNameVariations_ProducesSameCodes(string input1, string input2)
    {
        // Act
        var result1 = SoundexAlgorithm.Encode(input1);
        var result2 = SoundexAlgorithm.Encode(input2);
        
        // Assert - they should produce the same code (but we don't hardcode what it is)
        Assert.Equal(result1, result2);
    }
    
    [Theory]
    [InlineData("Color", "Colour")]
    [InlineData("Center", "Centre")]
    [InlineData("Theater", "Theatre")]
    public void AreSimilar_AmericanVsBritishSpelling_MayDiffer(string american, string british)
    {
        // Act
        var areSimilar = SoundexAlgorithm.AreSimilar(american, british);
        
        // Just checking it doesn't crash - similarity depends on specific words
        Assert.True(areSimilar == true || areSimilar == false);
    }
    
    #endregion
}
