using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

public class RoomRepository : BaseRepository<Room>, IRoomRepository
{
    public RoomRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Room>> GetPublicRoomsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Owner)
            .Where(r => r.Visibility == RoomVisibility.Public && r.Status == RoomStatus.Lobby)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> GetByStatusAsync(
        RoomStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Owner)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Room?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Owner)
            .Include(r => r.TagSelections)
                .ThenInclude(ts => ts.Tag)
            .Include(r => r.Sessions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Room?> GetByInvitationCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Invitations)
            .FirstOrDefaultAsync(r => r.Invitations.Any(i => i.Code == code && !i.IsExpired), cancellationToken);
    }
}
