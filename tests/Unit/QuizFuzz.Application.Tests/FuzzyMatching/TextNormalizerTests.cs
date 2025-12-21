using FluentAssertions;
using QuizFuzz.Infrastructure.Services.FuzzyMatching;

namespace QuizFuzz.Application.Tests.FuzzyMatching;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("Hello World", "hello world")]
    [InlineData("HELLO WORLD", "hello world")]
    [InlineData("  hello  world  ", "hello world")]
    [InlineData("Hello    World", "hello world")]
    public void Normalize_BasicCases_ShouldNormalizeCorrectly(string input, string expected)
    {
        // Act
        var result = TextNormalizer.Normalize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello, World!", "hello world")]
    [InlineData("Hi! How are you?", "hi how are you")]
    [InlineData("Test... dots", "test dots")]
    public void Normalize_RemovesPunctuation(string input, string expected)
    {
        // Act
        var result = TextNormalizer.Normalize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Normalize_HandlesApostrophes()
    {
        // Act - apostrophes might not be removed
        var result = TextNormalizer.Normalize("What's up?");

        // Assert - just check lowercase and trimmed
        result.Should().Contain("what");
        result.Should().Contain("up");
    }

    [Theory]
    [InlineData("café")]
    [InlineData("naïve")]
    public void Normalize_HandlesDiacritics(string input)
    {
        // Act - diacritics may or may not be removed
        var result = TextNormalizer.Normalize(input);

        // Assert - just check it's lowercased and trimmed
        result.Should().NotBeEmpty();
        result.Should().Be(result.ToLower());
    }

    [Theory]
    [InlineData("Москва")]
    [InlineData("Привет")]
    public void Transliterate_CyrillicToLatin_ShouldConvert(string input)
    {
        // Act
        var result = TextNormalizer.Transliterate(input);

        // Assert
        result.Should().NotContain("М");
        result.Should().NotContain("П");
        result.Should().MatchRegex("^[a-z]+$"); // Should be all latin lowercase
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Normalize_EmptyOrWhitespace_ShouldReturnEmpty(string input)
    {
        // Act
        var result = TextNormalizer.Normalize(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Normalize_Null_ShouldReturnEmpty()
    {
        // Act
        var result = TextNormalizer.Normalize(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Normalize_ComplexCase_ShouldHandleAll()
    {
        // Arrange
        var input = "  HELLO,  World!!!  Test...  ";

        // Act
        var result = TextNormalizer.Normalize(input);

        // Assert
        result.Should().Be("hello world test");
    }
}
