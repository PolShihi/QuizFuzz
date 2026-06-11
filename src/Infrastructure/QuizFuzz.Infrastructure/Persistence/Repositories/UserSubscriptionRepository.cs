using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

public class UserSubscriptionRepository : BaseRepository<UserSubscription>, IUserSubscriptionRepository
{
    public UserSubscriptionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<UserSubscription?> GetCurrentActiveByUserIdAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.UserId == userId && x.Status == UserSubscriptionStatus.Active && x.ExpiresAt > utcNow)
            .OrderByDescending(x => x.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSubscription>> GetActiveByUserIdsAsync(
        IEnumerable<Guid> userIds,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0)
            return Array.Empty<UserSubscription>();

        return await _dbSet
            .Where(x => ids.Contains(x.UserId) && x.Status == UserSubscriptionStatus.Active && x.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSubscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
