using FluentAssertions;
using QuizFuzz.Domain.ValueObjects;

namespace QuizFuzz.Domain.Tests.ValueObjects;

public class NormalizedTextTests
{
    [Theory]
    [InlineData("Hello", "hello")]
    [InlineData("HELLO", "hello")]
    [InlineData("  hello  ", "hello")]
    [InlineData("Hello World", "hello world")]
    [InlineData("Hello   World", "hello world")]
    public void Create_ShouldNormalizeText(string input, string expected)
    {
        // Act
        var normalized = NormalizedText.Create(input);

        // Assert
        normalized.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ShouldThrowArgumentException(string input)
    {
        // Act
        Action act = () => NormalizedText.Create(input);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNull_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => NormalizedText.Create(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equals_WithSameNormalizedValue_ShouldReturnTrue()
    {
        // Arrange
        var text1 = NormalizedText.Create("Hello World");
        var text2 = NormalizedText.Create("HELLO  WORLD");

        // Act & Assert
        text1.Should().Be(text2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var text1 = NormalizedText.Create("Hello");
        var text2 = NormalizedText.Create("Goodbye");

        // Act & Assert
        text1.Should().NotBe(text2);
    }

    [Fact]
    public void GetHashCode_WithSameNormalizedValue_ShouldReturnSameHash()
    {
        // Arrange
        var text1 = NormalizedText.Create("Hello");
        var text2 = NormalizedText.Create("HELLO");

        // Act & Assert
        text1.GetHashCode().Should().Be(text2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnNormalizedValue()
    {
        // Arrange
        var text = NormalizedText.Create("  HELLO  ");

        // Act
        var result = text.ToString();

        // Assert
        result.Should().Be("hello");
    }
}
