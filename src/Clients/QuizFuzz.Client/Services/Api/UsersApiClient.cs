using QuizFuzz.Shared.Dtos.Users;
using System.Net.Http.Json;

namespace QuizFuzz.Client.Services.Api;

public class UsersApiClient : IUsersApiClient
{
    private readonly HttpClient _httpClient;

    public UsersApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserStatsDto?> GetUserStatsAsync(Guid userId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserStatsDto>($"api/users/{userId}/stats");
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserStatsDto?> GetMyStatsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserStatsDto>("api/users/me/stats");
        }
        catch
        {
            return null;
        }
    }
}
