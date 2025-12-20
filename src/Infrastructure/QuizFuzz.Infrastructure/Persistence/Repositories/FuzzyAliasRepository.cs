using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

public class FuzzyAliasRepository : BaseRepository<FuzzyAlias>, IFuzzyAliasRepository
{
    public FuzzyAliasRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<FuzzyAlias>> GetByQuestionAnswerIdAsync(
        Guid questionAnswerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(fa => fa.QuestionAnswerId == questionAnswerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FuzzyAlias>> GetByQuestionIdAsync(
        Guid questionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(fa => fa.QuestionAnswer)
            .Where(fa => fa.QuestionAnswer.QuestionId == questionId)
            .ToListAsync(cancellationToken);
    }
}
