using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _dbSet
            .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeAllActiveByUserIdAsync(
        Guid userId,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default)
    {
        var tokens = await GetActiveByUserIdAsync(userId, cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(revokedByIp);
        }
    }
}
