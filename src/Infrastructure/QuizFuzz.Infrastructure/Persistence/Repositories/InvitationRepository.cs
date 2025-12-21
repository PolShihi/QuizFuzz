using Microsoft.EntityFrameworkCore;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Infrastructure.Persistence.Repositories;

public class InvitationRepository : BaseRepository<Invitation>, IInvitationRepository
{
    public InvitationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Invitation?> GetByCodeAsync(string code)
    {
        return await _context.Invitations
            .Include(i => i.Room)
                .ThenInclude(r => r.Owner)
            .FirstOrDefaultAsync(i => i.Code == code);
    }

    public async Task<IReadOnlyList<Invitation>> GetByRoomIdAsync(Guid roomId)
    {
        return await _context.Invitations
            .Where(i => i.RoomId == roomId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Invitation>> GetActiveByRoomIdAsync(Guid roomId)
    {
        return await _context.Invitations
            .Where(i => i.RoomId == roomId && i.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }
}
