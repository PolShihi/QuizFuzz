using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Application.Common.Interfaces.Persistence;

/// <summary>
/// Репозиторий для работы с комнатами
/// </summary>
public interface IRoomRepository : IRepository<Room>
{
    Task<IReadOnlyList<Room>> GetPublicRoomsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Room>> GetLobbyRoomsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Room>> GetLobbyRoomsForCleanupAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Room>> GetByStatusAsync(RoomStatus status, CancellationToken cancellationToken = default);
    Task<Room?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Room?> GetByInvitationCodeAsync(string code, CancellationToken cancellationToken = default);
}
