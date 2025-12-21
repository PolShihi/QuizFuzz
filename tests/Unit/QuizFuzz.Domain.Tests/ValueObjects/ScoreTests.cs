using FluentAssertions;
using QuizFuzz.Domain.ValueObjects;

namespace QuizFuzz.Domain.Tests.ValueObjects;

public class ScoreTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Create_WithValidScore_ShouldSucceed(int validScore)
    {
        // Act
        var score = Score.Create(validScore);

        // Assert
        score.Should().NotBeNull();
        score.Value.Should().Be(validScore);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithNegativeScore_ShouldThrowArgumentException(int negativeScore)
    {
        // Act
        Action act = () => Score.Create(negativeScore);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public void Add_ShouldReturnNewScoreWithSum()
    {
        // Arrange
        var score1 = Score.Create(50);
        var score2 = Score.Create(30);

        // Act
        var result = score1.Add(score2);

        // Assert
        result.Value.Should().Be(80);
        score1.Value.Should().Be(50); // Original should be unchanged
    }

    [Fact]
    public void Add_WithInteger_ShouldReturnNewScoreWithSum()
    {
        // Arrange
        var score = Score.Create(50);

        // Act
        var result = score.Add(25);

        // Assert
        result.Value.Should().Be(75);
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var score1 = Score.Create(100);
        var score2 = Score.Create(100);

        // Act & Assert
        score1.Should().Be(score2);
        (score1 == score2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var score1 = Score.Create(100);
        var score2 = Score.Create(50);

        // Act & Assert
        score1.Should().NotBe(score2);
        (score1 == score2).Should().BeFalse();
    }

    [Fact]
    public void CompareTo_ShouldWorkCorrectly()
    {
        // Arrange
        var score1 = Score.Create(50);
        var score2 = Score.Create(100);
        var score3 = Score.Create(50);

        // Act & Assert
        (score1 < score2).Should().BeTrue();
        (score2 > score1).Should().BeTrue();
        (score1 <= score3).Should().BeTrue();
        (score1 >= score3).Should().BeTrue();
    }

    [Fact]
    public void Zero_ShouldReturnScoreWithZeroValue()
    {
        // Act
        var zero = Score.Zero;

        // Assert
        zero.Value.Should().Be(0);
    }
}
