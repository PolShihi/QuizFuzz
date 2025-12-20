using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

/// <summary>
/// Репозиторий для работы с ответами игроков
/// </summary>
public interface IPlayerAnswerRepository : IRepository<PlayerAnswer>
{
    Task<IReadOnlyList<PlayerAnswer>> GetByRoundIdAsync(Guid roundId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlayerAnswer>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PlayerAnswer?> GetWithEvaluationAsync(Guid id, CancellationToken cancellationToken = default);
}
