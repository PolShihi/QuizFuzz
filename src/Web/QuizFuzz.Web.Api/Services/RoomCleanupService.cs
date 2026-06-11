using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Entities;
using QuizFuzz.Domain.Enums;
using QuizFuzz.Web.Api.Hubs;

namespace QuizFuzz.Web.Api.Services;

/// <summary>
/// Удаляет пустые и слишком долго ожидающие комнаты из лобби.
/// </summary>
public sealed class RoomCleanupService : IRoomCleanupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<LobbyHub> _lobbyHubContext;
    private readonly RoomCleanupOptions _options;
    private readonly ILogger<RoomCleanupService> _logger;

    public RoomCleanupService(
        IUnitOfWork unitOfWork,
        IHubContext<LobbyHub> lobbyHubContext,
        IOptions<RoomCleanupOptions> options,
        ILogger<RoomCleanupService> logger)
    {
        _unitOfWork = unitOfWork;
        _lobbyHubContext = lobbyHubContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return 0;

        var now = DateTime.UtcNow;
        var staleLobbyBefore = now.AddMinutes(-Math.Max(1, _options.LobbyLifetimeMinutes));
        var emptyRoomBefore = now.AddSeconds(-Math.Max(0, _options.EmptyRoomGracePeriodSeconds));

        var rooms = await _unitOfWork.Rooms.GetLobbyRoomsForCleanupAsync(cancellationToken);
        var roomsToRemove = new List<Room>();

        foreach (var room in rooms)
        {
            var currentSession = room.Sessions
                .Where(s => s.Status == GameSessionStatus.Pending || s.Status == GameSessionStatus.Active)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();

            var activePlayersCount = currentSession?.Players.Count(p => p.IsActive) ?? 0;
            var isEmpty = activePlayersCount == 0 && room.CreatedAt <= emptyRoomBefore;
            var isStaleLobby = room.CreatedAt <= staleLobbyBefore;

            if (!isEmpty && !isStaleLobby)
                continue;

            roomsToRemove.Add(room);

            _logger.LogInformation(
                "Room {RoomId} ('{RoomName}') scheduled for cleanup. Reason: {Reason}. Active players: {ActivePlayersCount}",
                room.Id,
                room.Name,
                isEmpty ? "empty" : "stale lobby",
                activePlayersCount);
        }

        if (roomsToRemove.Count == 0)
            return 0;

        _unitOfWork.Rooms.RemoveRange(roomsToRemove);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var room in roomsToRemove)
        {
            await _lobbyHubContext.Clients.Group("lobby").SendAsync("RoomRemoved", new
            {
                roomId = room.Id,
                timestamp = DateTime.UtcNow
            }, cancellationToken);
        }

        _logger.LogInformation("Room cleanup removed {Count} room(s)", roomsToRemove.Count);

        return roomsToRemove.Count;
    }
}
