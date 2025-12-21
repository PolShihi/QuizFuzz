using FluentAssertions;
using QuizFuzz.Domain.ValueObjects;

namespace QuizFuzz.Domain.Tests.ValueObjects;

public class TimeMsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(60000)]
    public void Create_WithValidTime_ShouldSucceed(int validTime)
    {
        // Act
        var time = TimeMs.Create(validTime);

        // Assert
        time.Should().NotBeNull();
        time.Value.Should().Be(validTime);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithNegativeTime_ShouldThrowArgumentException(int negativeTime)
    {
        // Act
        Action act = () => TimeMs.Create(negativeTime);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public void FromSeconds_ShouldConvertCorrectly()
    {
        // Act
        var time = TimeMs.FromSeconds(5);

        // Assert
        time.Value.Should().Be(5000);
        time.ToSeconds().Should().Be(5);
    }

    [Fact]
    public void ToSeconds_ShouldReturnCorrectValue()
    {
        // Arrange
        var time = TimeMs.Create(3500);

        // Act
        var seconds = time.ToSeconds();

        // Assert
        seconds.Should().Be(3);
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnValue()
    {
        // Arrange
        var time = TimeMs.Create(1500);

        // Act
        int value = time;

        // Assert
        value.Should().Be(1500);
    }

    [Fact]
    public void Zero_ShouldReturnTimeWithZeroValue()
    {
        // Act
        var zero = TimeMs.Zero;

        // Assert
        zero.Value.Should().Be(0);
    }

    [Fact]
    public void ToTimeSpan_ShouldConvertCorrectly()
    {
        // Arrange
        var time = TimeMs.Create(5000);

        // Act
        var timeSpan = time.ToTimeSpan();

        // Assert
        timeSpan.TotalMilliseconds.Should().Be(5000);
        timeSpan.TotalSeconds.Should().Be(5);
    }
}
