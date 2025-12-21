using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

public class ScoreboardRepository : BaseRepository<Scoreboard>, IScoreboardRepository
{
    public ScoreboardRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Scoreboard>> GetBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.User)
            .Where(s => s.SessionId == sessionId)
            .OrderByDescending(s => s.ScoreTotal)
            .ToListAsync(cancellationToken);
    }

    public async Task<Scoreboard?> GetBySessionAndUserAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(
                s => s.SessionId == sessionId && s.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Scoreboard>> GetLeaderboardAsync(
        Guid sessionId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.User)
            .Where(s => s.SessionId == sessionId)
            .OrderByDescending(s => s.ScoreTotal)
            .ThenByDescending(s => s.UniqueCorrectCount)
            .ThenByDescending(s => s.CorrectCount)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Scoreboard>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Session)
                .ThenInclude(s => s.Room)
            .Include(s => s.User)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Scoreboard>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.User)
            .Include(s => s.Session)
            .ToListAsync(cancellationToken);
    }
}
