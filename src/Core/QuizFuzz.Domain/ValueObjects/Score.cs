using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.ValueObjects;

/// <summary>
/// Value Object для очков игрока
/// </summary>
public sealed class Score : ValueObject
{
    public int Value { get; private set; }

    private Score(int value)
    {
        Value = value;
    }

    public static Score Create(int value)
    {
        if (value < 0)
            throw new ArgumentException("Score cannot be negative", nameof(value));

        return new Score(value);
    }

    public static Score Zero => new(0);

    public Score Add(int points)
    {
        if (points < 0)
            throw new ArgumentException("Cannot add negative points", nameof(points));

        return new Score(Value + points);
    }

    public Score Add(Score other)
    {
        return new Score(Value + other.Value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator int(Score score) => score.Value;
}
