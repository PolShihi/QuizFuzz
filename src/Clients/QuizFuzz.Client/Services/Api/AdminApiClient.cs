using QuizFuzz.Shared.Dtos.Admin;
using QuizFuzz.Shared.Dtos.Questions;
using System.Net.Http.Json;

namespace QuizFuzz.Client.Services.Api;

public class AdminApiClient : IAdminApiClient
{
    private readonly HttpClient _httpClient;

    public AdminApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<UserManagementDto>> GetUsersAsync()
    {
        try
        {
            var users = await _httpClient.GetFromJsonAsync<List<UserManagementDto>>("api/admin/users");
            return users ?? new List<UserManagementDto>();
        }
        catch
        {
            return new List<UserManagementDto>();
        }
    }

    public async Task<UserManagementDto?> GetUserAsync(Guid userId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserManagementDto>($"api/admin/users/{userId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateUserRolesAsync(UpdateUserRolesRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/admin/users/{request.UserId}/roles", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> BanUserAsync(BanUserRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/admin/users/{request.UserId}/ban", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnbanUserAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/admin/users/{userId}/unban", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<TagDto>> GetTagsAsync()
    {
        try
        {
            var tags = await _httpClient.GetFromJsonAsync<List<TagDto>>("api/tags");
            return tags ?? new List<TagDto>();
        }
        catch
        {
            return new List<TagDto>();
        }
    }

    public async Task<TagDto?> CreateTagAsync(string name, string? description)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/tags", new { name, description });
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<TagDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteTagAsync(Guid tagId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/tags/{tagId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
