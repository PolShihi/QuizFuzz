using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using QuizFuzz.Application.Common.Interfaces.Persistence;
using QuizFuzz.Domain.Enums;

namespace QuizFuzz.Web.Api.Hubs;

/// <summary>
/// SignalR Hub для лобби и обновлений комнат
/// </summary>
[Authorize]
public class LobbyHub : Hub
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LobbyHub> _logger;

    // Group name для лобби
    private const string LobbyGroupName = "lobby";

    public LobbyHub(
        IUnitOfWork unitOfWork,
        ILogger<LobbyHub> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Подключиться к лобби
    /// </summary>
    public async Task JoinLobby()
    {
        var userId = GetUserId();
        var username = Context.User?.Identity?.Name ?? "Unknown";

        await Groups.AddToGroupAsync(Context.ConnectionId, LobbyGroupName);

        _logger.LogDebug("User {UserId} ({Username}) joined lobby", userId, username);

        // Отправляем список публичных комнат
        var rooms = await _unitOfWork.Rooms.GetPublicRoomsAsync();

        var roomList = rooms.Select(r => new
        {
            id = r.Id,
            name = r.Name,
            ownerUsername = r.Owner.Username,
            maxPlayers = r.MaxPlayers,
            currentPlayers = 0, // TODO: подсчитать из активной сессии
            status = r.Status.ToString(),
            victoryConditionType = r.VictoryConditionType.ToString(),
            victoryValue = r.VictoryValue,
            createdAt = r.CreatedAt
        });

        await Clients.Caller.SendAsync("LobbyState", new
        {
            rooms = roomList,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Покинуть лобби
    /// </summary>
    public async Task LeaveLobby()
    {
        var userId = GetUserId();

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, LobbyGroupName);

        _logger.LogDebug("User {UserId} left lobby", userId);
    }

    /// <summary>
    /// Уведомить о создании комнаты
    /// </summary>
    public async Task NotifyRoomCreated(Guid roomId)
    {
        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(roomId);
        if (room == null)
            return;

        // Уведомляем только если комната публичная
        if (room.Visibility == RoomVisibility.Public)
        {
            await Clients.Group(LobbyGroupName).SendAsync("RoomCreated", new
            {
                id = room.Id,
                name = room.Name,
                ownerUsername = room.Owner.Username,
                maxPlayers = room.MaxPlayers,
                currentPlayers = 0,
                status = room.Status.ToString(),
                createdAt = room.CreatedAt
            });

            _logger.LogDebug("Notified lobby about new room {RoomId}", roomId);
        }
    }

    /// <summary>
    /// Уведомить об обновлении комнаты
    /// </summary>
    public async Task NotifyRoomUpdated(Guid roomId)
    {
        var room = await _unitOfWork.Rooms.GetWithDetailsAsync(roomId);
        if (room == null)
            return;

        if (room.Visibility == RoomVisibility.Public)
        {
            await Clients.Group(LobbyGroupName).SendAsync("RoomUpdated", new
            {
                id = room.Id,
                name = room.Name,
                status = room.Status.ToString(),
                currentPlayers = 0, // TODO: подсчитать
                updatedAt = room.UpdatedAt
            });

            _logger.LogDebug("Notified lobby about room update {RoomId}", roomId);
        }
    }

    /// <summary>
    /// Уведомить об удалении/закрытии комнаты
    /// </summary>
    public async Task NotifyRoomRemoved(Guid roomId)
    {
        await Clients.Group(LobbyGroupName).SendAsync("RoomRemoved", new
        {
            roomId,
            timestamp = DateTime.UtcNow
        });

        _logger.LogDebug("Notified lobby about room removal {RoomId}", roomId);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var username = Context.User?.Identity?.Name ?? "Anonymous";
        var connectionId = Context.ConnectionId;
        
        _logger.LogDebug(" [LobbyHub] CONNECTED - ConnectionId: {ConnectionId}, UserId: {UserId}, Username: {Username}", 
            connectionId, userId, username);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;
        
        _logger.LogDebug(" [LobbyHub] DISCONNECTED - ConnectionId: {ConnectionId}, UserId: {UserId}", 
            connectionId, userId);
        
        if (exception != null)
        {
            _logger.LogDebug(exception, " [LobbyHub] Disconnection error for ConnectionId: {ConnectionId}", connectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
    
    /// <summary>
    /// Присоединиться к группе комнаты для получения обновлений
    /// </summary>
    public async Task JoinRoomGroup(string roomId)
    {
        var userId = GetUserId();
        var groupName = $"Room_{roomId}";
        
        _logger.LogDebug(" [LobbyHub] JoinRoomGroup - ConnectionId: {ConnectionId}, UserId: {UserId}, RoomId: {RoomId}", 
            Context.ConnectionId, userId, roomId);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogDebug(" [LobbyHub] Joined room group: {GroupName}", groupName);
    }

    /// <summary>
    /// Покинуть группу комнаты
    /// </summary>
    public async Task LeaveRoomGroup(string roomId)
    {
        var groupName = $"Room_{roomId}";
        
        _logger.LogDebug(" [LobbyHub] LeaveRoomGroup - ConnectionId: {ConnectionId}, RoomId: {RoomId}", 
            Context.ConnectionId, roomId);
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogDebug(" [LobbyHub] Left room group: {GroupName}", groupName);
    }

    private Guid GetUserId()
    {
        var userIdString = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
    }
}
