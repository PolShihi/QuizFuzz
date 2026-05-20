using QuizFuzz.Shared.Dtos.Rooms;
using System.Net.Http.Json;

namespace QuizFuzz.Client.Services.Api;

public class RoomsApiClient : IRoomsApiClient
{
    private readonly HttpClient _httpClient;

    public RoomsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<RoomListItemDto>> GetRoomsAsync()
    {
        try
        {
            Console.WriteLine("📤 [RoomsApiClient] GET api/rooms");
            var rooms = await _httpClient.GetFromJsonAsync<List<RoomListItemDto>>("api/rooms");
            
            if (rooms != null && rooms.Any())
            {
                Console.WriteLine($"📥 [RoomsApiClient] Received {rooms.Count} rooms:");
                foreach (var room in rooms)
                {
                    Console.WriteLine($"   📍 {room.Name}: {room.CurrentPlayers}/{room.MaxPlayers} players, Status: {room.Status}");
                }
            }
            else
            {
                Console.WriteLine("📥 [RoomsApiClient] No rooms received");
            }
            
            return rooms ?? new List<RoomListItemDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [RoomsApiClient] Error loading rooms: {ex.Message}");
            return new List<RoomListItemDto>();
        }
    }

    public async Task<RoomDetailsDto?> GetRoomAsync(Guid roomId)
    {
        try
        {
            Console.WriteLine($"📥 [RoomsApiClient] Loading room details: {roomId}");
            var room = await _httpClient.GetFromJsonAsync<RoomDetailsDto>($"api/rooms/{roomId}");
            
            if (room != null)
            {
                Console.WriteLine($"✅ [RoomsApiClient] Room details loaded:");
                Console.WriteLine($"   Name: {room.Name}");
                Console.WriteLine($"   Players: {room.Players?.Count ?? 0}/{room.MaxPlayers}");
                Console.WriteLine($"   Status: {room.Status}");
            }
            else
            {
                Console.WriteLine($"❌ [RoomsApiClient] Room details is null");
            }
            
            return room;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [RoomsApiClient] Error loading room: {ex.Message}");
            return null;
        }
    }

    public async Task<RoomListItemDto?> CreateRoomAsync(CreateRoomRequest request)
    {
        try
        {
            Console.WriteLine($"📤 [RoomsApiClient] Creating room: {request.Name}");
            Console.WriteLine($"   Max Players: {request.MaxPlayers}");
            Console.WriteLine($"   Tags: {request.TagSelections?.Count ?? 0}");
            
            var response = await _httpClient.PostAsJsonAsync("api/rooms", request);
            
            Console.WriteLine($"📥 [RoomsApiClient] CREATE response: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ [RoomsApiClient] CREATE failed: {errorContent}");
                return null;
            }

            var room = await response.Content.ReadFromJsonAsync<RoomListItemDto>();
            Console.WriteLine($"✅ [RoomsApiClient] Room created: {room?.Id}");
            Console.WriteLine($"   Current Players: {room?.CurrentPlayers}");
            
            return room;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [RoomsApiClient] Exception in CreateRoomAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> JoinRoomAsync(Guid roomId)
    {
        try
        {
            Console.WriteLine($"📤 [RoomsApiClient] Sending JOIN request to: api/rooms/{roomId}/join");
            var response = await _httpClient.PostAsync($"api/rooms/{roomId}/join", null);
            
            Console.WriteLine($"📥 [RoomsApiClient] JOIN response: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ [RoomsApiClient] JOIN failed: {response.StatusCode}");
                Console.WriteLine($"❌ [RoomsApiClient] Error: {errorContent}");
                return false;
            }
            
            Console.WriteLine($"✅ [RoomsApiClient] Successfully joined room {roomId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [RoomsApiClient] Exception in JoinRoomAsync: {ex.Message}");
            Console.WriteLine($"❌ [RoomsApiClient] Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> LeaveRoomAsync(Guid roomId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/rooms/{roomId}/leave", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> StartGameAsync(Guid roomId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/rooms/{roomId}/start", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
