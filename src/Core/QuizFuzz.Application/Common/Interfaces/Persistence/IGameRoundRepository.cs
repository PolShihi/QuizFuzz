using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

/// <summary>
/// Репозиторий для работы с игровыми раундами
/// </summary>
public interface IGameRoundRepository : IRepository<GameRound>
{
    Task<GameRound?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameRound>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<GameRound?> GetActiveRoundBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
