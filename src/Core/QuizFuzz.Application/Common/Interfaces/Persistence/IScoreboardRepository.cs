using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

/// <summary>
/// Репозиторий для работы с таблицей очков
/// </summary>
public interface IScoreboardRepository : IRepository<Scoreboard>
{
    Task<IReadOnlyList<Scoreboard>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<Scoreboard?> GetBySessionAndUserAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Scoreboard>> GetLeaderboardAsync(Guid sessionId, int limit = 10, CancellationToken cancellationToken = default);
}
