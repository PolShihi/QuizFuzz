using QuizFuzz.Domain.Common;

namespace QuizFuzz.Domain.Entities;

/// <summary>
/// Связь между вопросом и тегом (many-to-many)
/// </summary>
public class QuestionTag
{
    public Guid QuestionId { get; private set; }
    public Guid TagId { get; private set; }

    // Navigation properties
    public Question Question { get; private set; } = null!;
    public Tag Tag { get; private set; } = null!;

    private QuestionTag() { } // EF Core

    public QuestionTag(Guid questionId, Guid tagId)
    {
        QuestionId = questionId;
        TagId = tagId;
    }
}
