using QuizFuzz.Domain.Entities;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

public interface IInvitationRepository : IRepository<Invitation>
{
    Task<Invitation?> GetByCodeAsync(string code);
    Task<IReadOnlyList<Invitation>> GetByRoomIdAsync(Guid roomId);
    Task<IReadOnlyList<Invitation>> GetActiveByRoomIdAsync(Guid roomId);
}
