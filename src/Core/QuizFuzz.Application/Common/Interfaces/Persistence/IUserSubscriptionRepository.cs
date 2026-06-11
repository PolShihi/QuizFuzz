using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

public interface IUserSubscriptionRepository : IRepository<UserSubscription>
{
    Task<UserSubscription?> GetCurrentActiveByUserIdAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSubscription>> GetActiveByUserIdsAsync(IEnumerable<Guid> userIds, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSubscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
