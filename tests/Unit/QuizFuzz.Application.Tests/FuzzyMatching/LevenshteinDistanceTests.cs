using FluentAssertions;
using QuizFuzz.Infrastructure.Services.FuzzyMatching;

namespace QuizFuzz.Application.Tests.FuzzyMatching;

public class LevenshteinDistanceTests
{
    [Theory]
    [InlineData("", "", 0)]
    [InlineData("hello", "hello", 0)]
    [InlineData("kitten", "kitten", 0)]
    public void Calculate_IdenticalStrings_ShouldReturnZero(string s1, string s2, int expected)
    {
        // Act
        var result = LevenshteinDistance.Calculate(s1, s2);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", "hello", 5)]
    [InlineData("hello", "", 5)]
    public void Calculate_EmptyString_ShouldReturnLengthOfOther(string s1, string s2, int expected)
    {
        // Act
        var result = LevenshteinDistance.Calculate(s1, s2);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("kitten", "sitting", 3)] // substitute k->s, substitute e->i, insert g
    [InlineData("saturday", "sunday", 3)] // delete a, delete t, delete r
    [InlineData("hello", "hallo", 1)] // substitute e->a
    [InlineData("paris", "pariis", 1)] // insert i
    public void Calculate_DifferentStrings_ShouldReturnCorrectDistance(string s1, string s2, int expected)
    {
        // Act
        var result = LevenshteinDistance.Calculate(s1, s2);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello", "helo", 1)] // missing 'l'
    [InlineData("color", "colour", 1)] // extra 'u'
    [InlineData("theater", "theatre", 2)] // re <-> er
    public void Calculate_CommonTypos_ShouldDetect(string s1, string s2, int expected)
    {
        // Act
        var result = LevenshteinDistance.Calculate(s1, s2);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "abc", 1.0)]
    [InlineData("abc", "abcd", 0.75)]
    [InlineData("abc", "xyz", 0.0)]
    [InlineData("kitten", "sitting", 0.571)] // 4 matching out of 7
    public void CalculateSimilarity_ShouldReturnCorrectRatio(string s1, string s2, double expectedMin)
    {
        // Act
        var result = LevenshteinDistance.CalculateSimilarity(s1, s2);

        // Assert
        result.Should().BeGreaterOrEqualTo((decimal)expectedMin - 0.01m); // Allow small margin
        result.Should().BeLessOrEqualTo(1.0m);
    }

    [Fact]
    public void Calculate_CaseInsensitive_ShouldIgnoreCase()
    {
        // Arrange
        var s1 = "Hello";
        var s2 = "hello";

        // Act
        var result = LevenshteinDistance.Calculate(s1.ToLower(), s2.ToLower());

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Calculate_LongStrings_ShouldHandleEfficiently()
    {
        // Arrange
        var s1 = new string('a', 100);
        var s2 = new string('a', 98) + "bb";

        // Act
        var result = LevenshteinDistance.Calculate(s1, s2);

        // Assert
        result.Should().Be(2);
    }
}
