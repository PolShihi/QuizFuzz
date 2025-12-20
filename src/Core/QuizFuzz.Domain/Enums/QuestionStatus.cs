namespace QuizFuzz.Domain.Enums;

/// <summary>
/// Статус вопроса в процессе модерации
/// </summary>
public enum QuestionStatus
{
    Draft = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    Archived = 5
}
