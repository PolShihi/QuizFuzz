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
        Console.WriteLine($"🔍 [GameRoundRepo] GetWithDetailsAsync - RoundId: {id}");
        
        var round = await _dbSet
            .Include(gr => gr.Question)
                .ThenInclude(q => q.Answers)
                    .ThenInclude(a => a.Aliases)
            .Include(gr => gr.Question)
                .ThenInclude(q => q.Hints)
            .Include(gr => gr.Question)
                .ThenInclude(q => q.MediaAssets) // 🔥 КРИТИЧНО: загружаем MediaAssets!
            .Include(gr => gr.Answers)
                .ThenInclude(a => a.User)
            .Include(gr => gr.Answers)
                .ThenInclude(a => a.Evaluation)
            .Include(gr => gr.Winner)
            .FirstOrDefaultAsync(gr => gr.Id == id, cancellationToken);
            
        if (round != null)
        {
            Console.WriteLine($"✅ [GameRoundRepo] Round found with {round.Question.MediaAssets.Count} MediaAssets");
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
        Console.WriteLine($"🔍 [GameRoundRepo] GetActiveRoundBySessionIdAsync - SessionId: {sessionId}");
        
        var round = await _dbSet
            .Include(gr => gr.Question)
                .ThenInclude(q => q.Answers)
                    .ThenInclude(a => a.Aliases)
            .Include(gr => gr.Question)
                .ThenInclude(q => q.Hints)
            .Include(gr => gr.Question)
                .ThenInclude(q => q.MediaAssets) // 🔥 КРИТИЧНО: загружаем MediaAssets!
            .FirstOrDefaultAsync(
                gr => gr.SessionId == sessionId && gr.Status == RoundStatus.Active,
                cancellationToken);
                
        if (round != null)
        {
            Console.WriteLine($"✅ [GameRoundRepo] Active round found: {round.Id}");
            Console.WriteLine($"   📝 Question: {round.Question.PromptText}");
            Console.WriteLine($"   🎯 Type: {round.Question.Type}");
            Console.WriteLine($"   📎 MediaAssets count: {round.Question.MediaAssets.Count}");
            
            if (round.Question.MediaAssets.Any())
            {
                foreach (var media in round.Question.MediaAssets)
                {
                    Console.WriteLine($"      🔗 {media.MediaType}: {media.Url}");
                }
            }
            else
            {
                Console.WriteLine($"   ⚠️ No MediaAssets for this question");
            }
        }
        else
        {
            Console.WriteLine($"❌ [GameRoundRepo] No active round found for session {sessionId}");
        }
        
        return round;
    }
}
