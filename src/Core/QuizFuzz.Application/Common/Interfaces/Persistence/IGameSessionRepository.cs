using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

/// <summary>
/// Репозиторий для работы с игровыми сессиями
/// </summary>
public interface IGameSessionRepository : IRepository<GameSession>
{
    Task<GameSession?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GameSession?> GetActiveByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameSession>> GetByStatusAsync(GameSessionStatus status, CancellationToken cancellationToken = default);
}
