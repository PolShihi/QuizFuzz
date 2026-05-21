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
            var rooms = await _httpClient.GetFromJsonAsync<List<RoomListItemDto>>("api/rooms");
            
            if (rooms != null && rooms.Any())
            {
                foreach (var room in rooms)
                {
                }
            }
            else
            {
            }
            
            return rooms ?? new List<RoomListItemDto>();
        }
        catch (Exception ex)
        {
            return new List<RoomListItemDto>();
        }
    }

    public async Task<RoomDetailsDto?> GetRoomAsync(Guid roomId)
    {
        try
        {
            var room = await _httpClient.GetFromJsonAsync<RoomDetailsDto>($"api/rooms/{roomId}");
            
            if (room != null)
            {
            }
            else
            {
            }
            
            return room;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<RoomListItemDto?> CreateRoomAsync(CreateRoomRequest request)
    {
        try
        {
            
            var response = await _httpClient.PostAsJsonAsync("api/rooms", request);
            
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return null;
            }

            var room = await response.Content.ReadFromJsonAsync<RoomListItemDto>();
            
            return room;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<bool> JoinRoomAsync(Guid roomId, string? accessCode = null)
    {
        try
        {
            HttpResponseMessage response;
            if (string.IsNullOrWhiteSpace(accessCode))
            {
                response = await _httpClient.PostAsync($"api/rooms/{roomId}/join", null);
            }
            else
            {
                response = await _httpClient.PostAsJsonAsync($"api/rooms/{roomId}/join", new { AccessCode = accessCode.Trim() });
            }
            
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
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
