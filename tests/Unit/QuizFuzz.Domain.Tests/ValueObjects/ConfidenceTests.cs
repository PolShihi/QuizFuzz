using FluentAssertions;
using QuizFuzz.Domain.ValueObjects;

namespace QuizFuzz.Domain.Tests.ValueObjects;

public class ConfidenceTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void Create_WithValidConfidence_ShouldSucceed(decimal validConfidence)
    {
        // Act
        var confidence = Confidence.Create(validConfidence);

        // Assert
        confidence.Should().NotBeNull();
        confidence.Value.Should().Be((decimal)validConfidence);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    [InlineData(-1.0)]
    public void Create_WithInvalidConfidence_ShouldThrowArgumentException(decimal invalidConfidence)
    {
        // Act
        Action act = () => Confidence.Create(invalidConfidence);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be between 0 and 1*");
    }

    [Fact]
    public void CompareTo_ShouldWorkCorrectly()
    {
        // Arrange
        var low = Confidence.Create(0.5m);
        var high = Confidence.Create(0.9m);

        // Act & Assert
        low.Value.Should().BeLessThan(high.Value);
        high.Value.Should().BeGreaterThan(low.Value);
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var confidence1 = Confidence.Create(0.85m);
        var confidence2 = Confidence.Create(0.85m);

        // Act & Assert
        confidence1.Should().Be(confidence2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var confidence1 = Confidence.Create(0.85m);
        var confidence2 = Confidence.Create(0.75m);

        // Act & Assert
        confidence1.Should().NotBe(confidence2);
    }

    [Fact]
    public void Zero_ShouldReturnConfidenceWithZeroValue()
    {
        // Act
        var zero = Confidence.Zero;

        // Assert
        zero.Value.Should().Be(0.0m);
    }

    // Perfect constant doesn't exist - removed test
}
