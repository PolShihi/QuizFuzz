using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

public class GameSessionRepository : BaseRepository<GameSession>, IGameSessionRepository
{
    public GameSessionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<GameSession?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(gs => gs.Room)
            .Include(gs => gs.Rounds)
                .ThenInclude(r => r.Question)
            .Include(gs => gs.Players)
                .ThenInclude(p => p.User)
            .Include(gs => gs.Scoreboards)
                .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(gs => gs.Id == id, cancellationToken);
    }

    public async Task<GameSession?> GetActiveByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(gs => gs.Rounds)
            .Include(gs => gs.Players)
            .Include(gs => gs.Scoreboards)
            .FirstOrDefaultAsync(
                gs => gs.RoomId == roomId && gs.Status == GameSessionStatus.Active,
                cancellationToken);
    }

    public async Task<IReadOnlyList<GameSession>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(gs => gs.Room)
            .Include(gs => gs.Players)
            .Where(gs => gs.Players.Any(p => p.UserId == userId))
            .OrderByDescending(gs => gs.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GameSession>> GetByStatusAsync(
        GameSessionStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(gs => gs.Room)
            .Where(gs => gs.Status == status)
            .OrderByDescending(gs => gs.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
