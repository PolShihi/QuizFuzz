using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

public class GameRoundRepository : BaseRepository<GameRound>, IGameRoundRepository
{
    public GameRoundRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<GameRound?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        
        var round = await _dbSet
            .Include(gr => gr.Question)
                .ThenInclude(q => q.Answers)
                    .ThenInclude(a => a.Aliases)
            .Include(gr => gr.Question)
                .ThenInclude(q => q.Hints)
            .Include(gr => gr.Question)
                .ThenInclude(q => q.MediaAssets) //  КРИТИЧНО: загружаем MediaAssets!
            .Include(gr => gr.Answers)
                .ThenInclude(a => a.User)
            .Include(gr => gr.Answers)
                .ThenInclude(a => a.Evaluation)
            .Include(gr => gr.Winner)
            .FirstOrDefaultAsync(gr => gr.Id == id, cancellationToken);
            
        if (round != null)
        {
        }
        
        return round;
    }

    public async Task<IReadOnlyList<GameRound>> GetBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(gr => gr.Question)
            .Where(gr => gr.SessionId == sessionId)
            .OrderBy(gr => gr.RoundIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<GameRound?> GetActiveRoundBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        
        var round = await _dbSet
            .Include(gr => gr.Question)
                .ThenInclude(q => q.Answers)
                    .ThenInclude(a => a.Aliases)
            .Include(gr => gr.Question)
                .ThenInclude(q => q.Hints)
            .Include(gr => gr.Question)
                .ThenInclude(q => q.MediaAssets) //  КРИТИЧНО: загружаем MediaAssets!
            .FirstOrDefaultAsync(
                gr => gr.SessionId == sessionId && gr.Status == RoundStatus.Active,
                cancellationToken);
                
        if (round != null)
        {
            
            if (round.Question.MediaAssets.Any())
            {
                foreach (var media in round.Question.MediaAssets)
                {
                }
            }
            else
            {
            }
        }
        else
        {
        }
        
        return round;
    }
}
