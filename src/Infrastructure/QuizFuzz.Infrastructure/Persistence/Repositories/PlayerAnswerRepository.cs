using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

public class PlayerAnswerRepository : BaseRepository<PlayerAnswer>, IPlayerAnswerRepository
{
    public PlayerAnswerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<PlayerAnswer>> GetByRoundIdAsync(
        Guid roundId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pa => pa.User)
            .Include(pa => pa.Evaluation)
            .Where(pa => pa.RoundId == roundId)
            .OrderBy(pa => pa.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerAnswer>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pa => pa.Round)
                .ThenInclude(r => r.Question)
            .Include(pa => pa.Evaluation)
            .Where(pa => pa.UserId == userId)
            .OrderByDescending(pa => pa.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlayerAnswer?> GetWithEvaluationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pa => pa.Evaluation)
            .Include(pa => pa.User)
            .Include(pa => pa.Round)
            .FirstOrDefaultAsync(pa => pa.Id == id, cancellationToken);
    }
}
