using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

/// <summary>
/// Репозиторий для работы с ответами на вопросы
/// </summary>
public interface IQuestionAnswerRepository : IRepository<QuestionAnswer>
{
    Task<IReadOnlyList<QuestionAnswer>> GetByQuestionIdAsync(Guid questionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuestionAnswer>> GetWithAliasesAsync(Guid questionId, CancellationToken cancellationToken = default);
    Task<QuestionAnswer?> GetByIdWithAliasesAsync(Guid id, CancellationToken cancellationToken = default);
}
