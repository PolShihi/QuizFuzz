using QuizFuzz.Shared.Dtos.Rooms;

namespace QuizFuzz.Client.Services.Api;

public interface IRoomsApiClient
{
    Task<List<RoomListItemDto>> GetRoomsAsync();
    Task<RoomDetailsDto?> GetRoomAsync(Guid roomId);
    Task<RoomListItemDto?> CreateRoomAsync(CreateRoomRequest request);
    Task<bool> JoinRoomAsync(Guid roomId, string? accessCode = null);
    Task<bool> LeaveRoomAsync(Guid roomId);
    Task<bool> StartGameAsync(Guid roomId);
}
