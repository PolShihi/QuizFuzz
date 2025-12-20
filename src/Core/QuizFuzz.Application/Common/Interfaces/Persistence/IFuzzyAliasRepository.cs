using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

/// <summary>
/// Репозиторий для работы с алиасами
/// </summary>
public interface IFuzzyAliasRepository : IRepository<FuzzyAlias>
{
    Task<IReadOnlyList<FuzzyAlias>> GetByQuestionAnswerIdAsync(Guid questionAnswerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FuzzyAlias>> GetByQuestionIdAsync(Guid questionId, CancellationToken cancellationToken = default);
}
