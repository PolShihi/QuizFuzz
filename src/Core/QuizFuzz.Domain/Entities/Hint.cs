using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Подсказка для вопроса
/// </summary>
public class Hint : BaseEntity
{
    public Guid QuestionId { get; private set; }
    public int OrderIndex { get; private set; }
    public string? HintText { get; private set; }
    public Guid? MediaAssetId { get; private set; }
    public int RevealTimeSec { get; private set; }

    // Navigation properties
    public Question Question { get; private set; } = null!;
    public MediaAsset? MediaAsset { get; private set; }

    private Hint() { } // EF Core

    public Hint(Guid questionId, int orderIndex, string? hintText, int revealTimeSec, Guid? mediaAssetId = null)
    {
        if (string.IsNullOrWhiteSpace(hintText) && mediaAssetId == null)
            throw new ArgumentException("Hint must have either text or media asset");

        if (revealTimeSec < 0)
            throw new ArgumentException("Reveal time cannot be negative", nameof(revealTimeSec));

        QuestionId = questionId;
        OrderIndex = orderIndex;
        HintText = hintText?.Trim();
        MediaAssetId = mediaAssetId;
        RevealTimeSec = revealTimeSec;
    }

    public void UpdateHintText(string? hintText)
    {
        if (string.IsNullOrWhiteSpace(hintText) && MediaAssetId == null)
            throw new ArgumentException("Hint must have either text or media asset");

        HintText = hintText?.Trim();
    }

    public void UpdateRevealTime(int revealTimeSec)
    {
        if (revealTimeSec < 0)
            throw new ArgumentException("Reveal time cannot be negative", nameof(revealTimeSec));

        RevealTimeSec = revealTimeSec;
    }

    public void UpdateOrderIndex(int orderIndex)
    {
        OrderIndex = orderIndex;
    }
}
