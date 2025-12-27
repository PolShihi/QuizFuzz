using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

/// <summary>
/// Репозиторий для работы с вопросами
/// </summary>
public interface IQuestionRepository : IRepository<Question>
{
    Task<Question?> GetWithAnswersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Question?> GetWithAllDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Question>> GetByStatusAsync(QuestionStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Question>> GetByTagsAsync(IEnumerable<Guid> tagIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Question>> GetApprovedByTagsAsync(IEnumerable<Guid> tagIds, int limit = 100, CancellationToken cancellationToken = default);
    Task<Question?> GetRandomApprovedAsync(IEnumerable<Guid>? tagIds = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Получить случайный одобренный вопрос с фильтрацией по тегам и сложности
    /// </summary>
    Task<Question?> GetRandomApprovedWithFiltersAsync(
        IEnumerable<Guid>? tagIds = null, 
        IEnumerable<Difficulty>? difficulties = null, 
        CancellationToken cancellationToken = default);
}
