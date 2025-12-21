using FluentAssertions;
using QuizFuzz.Infrastructure.Services.FuzzyMatching;

namespace QuizFuzz.Application.Tests.FuzzyMatching;

public class TokenSimilarityTests
{
    [Theory]
    [InlineData("hello world", "hello world", 1.0)]
    [InlineData("hello world", "world hello", 1.0)] // Order shouldn't matter
    [InlineData("the quick brown fox", "brown fox the quick", 1.0)]
    public void Calculate_SameTokens_ShouldReturn100(string s1, string s2, decimal expected)
    {
        // Act
        var result = TokenSimilarity.CalculateSetRatio(s1, s2);

        // Assert
        result.Should().BeGreaterOrEqualTo(expected - 0.01m);
    }

    [Theory]
    [InlineData("hello world", "hello", 0.5)] // 2 tokens vs 1
    [InlineData("hello world test", "hello world", 0.6)] // 3 tokens vs 2
    public void Calculate_PartialMatch_ShouldReturnCorrectRatio(string s1, string s2, decimal expectedMin)
    {
        // Act
        var result = TokenSimilarity.CalculateSetRatio(s1, s2);

        // Assert
        result.Should().BeGreaterOrEqualTo(expectedMin);
    }

    [Theory]
    [InlineData("hello world", "goodbye universe", 0.0)]
    [InlineData("abc def", "xyz qwe", 0.0)]
    public void Calculate_NoMatch_ShouldReturnZero(string s1, string s2, decimal expected)
    {
        // Act
        var result = TokenSimilarity.CalculateSetRatio(s1, s2);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("new york city", "new york", 0.6)]
    [InlineData("united states of america", "united states", 0.4)]
    public void Calculate_MultiWordPhrases_ShouldWork(string s1, string s2, decimal expectedMin)
    {
        // Act
        var result = TokenSimilarity.CalculateSetRatio(s1, s2);

        // Assert
        result.Should().BeGreaterOrEqualTo(expectedMin);
    }

    [Theory]
    [InlineData("  hello   world  ", "hello world")]
    [InlineData("HELLO WORLD", "hello world")]
    public void Calculate_ShouldHandleWhitespaceAndCase(string s1, string s2)
    {
        // Act
        var result = TokenSimilarity.CalculateSetRatio(s1, s2);

        // Assert
        result.Should().BeGreaterThan(0.95m);
    }

    [Theory]
    [InlineData("", "hello")]
    [InlineData("hello", "")]
    public void Calculate_EmptyStrings_ShouldReturnZero(string s1, string s2)
    {
        // Act
        var result = TokenSimilarity.CalculateSetRatio(s1, s2);

        // Assert
        result.Should().Be(0.0m);
    }

    [Fact]
    public void Calculate_BothEmpty_ShouldReturnOne()
    {
        // Act - both empty is considered 100% match
        var result = TokenSimilarity.CalculateSetRatio("", "");

        // Assert
        result.Should().Be(1.0m);
    }

    [Fact]
    public void Calculate_DuplicateTokens_ShouldHandleCorrectly()
    {
        // Arrange
        var s1 = "hello hello world";
        var s2 = "hello world world";

        // Act
        var result = TokenSimilarity.CalculateSetRatio(s1, s2);

        // Assert
        result.Should().BeGreaterThan(0.8m);
    }
}



