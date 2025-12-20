using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.ValueObjects;

/// <summary>
/// Value Object для времени в миллисекундах
/// </summary>
public sealed class TimeMs : ValueObject
{
    public int Value { get; private set; }

    private TimeMs(int value)
    {
        Value = value;
    }

    public static TimeMs Create(int milliseconds)
    {
        if (milliseconds < 0)
            throw new ArgumentException("Time cannot be negative", nameof(milliseconds));

        return new TimeMs(milliseconds);
    }

    public static TimeMs FromSeconds(int seconds)
    {
        if (seconds < 0)
            throw new ArgumentException("Seconds cannot be negative", nameof(seconds));

        return new TimeMs(seconds * 1000);
    }

    public static TimeMs Zero => new(0);

    public int ToSeconds() => Value / 1000;

    public TimeSpan ToTimeSpan() => TimeSpan.FromMilliseconds(Value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value}ms";

    public static implicit operator int(TimeMs time) => time.Value;
}
