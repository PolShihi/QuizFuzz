using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RevokeAllActiveByUserIdAsync(Guid userId, string? revokedByIp = null, CancellationToken cancellationToken = default);
}
