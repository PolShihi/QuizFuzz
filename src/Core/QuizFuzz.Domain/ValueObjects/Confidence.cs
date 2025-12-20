using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.ValueObjects;

/// <summary>
/// Value Object для уверенности в правильности ответа (0.0 - 1.0)
/// </summary>
public sealed class Confidence : ValueObject
{
    public decimal Value { get; private set; }

    private Confidence(decimal value)
    {
        Value = value;
    }

    public static Confidence Create(decimal value)
    {
        if (value < 0 || value > 1)
            throw new ArgumentException("Confidence must be between 0 and 1", nameof(value));

        return new Confidence(value);
    }

    public static Confidence Zero => new(0);
    public static Confidence Full => new(1);

    public bool IsHigh => Value >= 0.8m;
    public bool IsMedium => Value >= 0.5m && Value < 0.8m;
    public bool IsLow => Value < 0.5m;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value:P0}";
}
