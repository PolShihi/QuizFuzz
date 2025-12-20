using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

public class QuestionAnswerRepository : BaseRepository<QuestionAnswer>, IQuestionAnswerRepository
{
    public QuestionAnswerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<QuestionAnswer>> GetByQuestionIdAsync(
        Guid questionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(qa => qa.QuestionId == questionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QuestionAnswer>> GetWithAliasesAsync(
        Guid questionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(qa => qa.Aliases)
            .Where(qa => qa.QuestionId == questionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<QuestionAnswer?> GetByIdWithAliasesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(qa => qa.Aliases)
            .FirstOrDefaultAsync(qa => qa.Id == id, cancellationToken);
    }
}
